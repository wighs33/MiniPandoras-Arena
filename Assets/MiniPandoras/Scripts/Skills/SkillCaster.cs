using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 인물 한 명의 스킬 쓰기. 마나와 재사용 대기를 세고, 쓸 수 있으면 인물에게 몸짓을 시키고(<see cref="Fighter.BeginSkill"/>),
    /// 정해진 시각에 기술을 내보낸다. 무엇이 나가는지(탄, 장판, 도약, 덫, 흘리기, 순간이동 …)는 스킬의 모양(<see cref="SkillKind"/>)이 정한다.
    /// 조작하는 쪽(사람·AI)은 <see cref="Use"/>를 부르고, 차원이동으로 숨은 동안은 <see cref="ActiveGate"/>로 나갈 곳을 고른다.
    /// </summary>
    public sealed class SkillCaster : MonoBehaviour
    {
        // 도약이 상대 몸과 겹치지 않게 이만큼(m, 몸 가운데끼리) 앞에서 내린다.
        private const float LandingGap = 0.7f;
        // 도약에서 내린 뒤 조금 더 맞지 않는 시간(초). 2D판은 날아가는 프레임 + 6프레임이다.
        private const float LandingGrace = 0.1f;
        // 차원참으로 상대 뒤에 나타난 뒤 맞지 않는 시간(초). 2D판의 14프레임.
        private const float WarpGrace = 0.23f;

        [SerializeField] private Fighter fighter;
        [SerializeField] private PandoraDefinition pandora;
        [Tooltip("투기장 가운데에서 순간이동·차원이동으로 나타날 수 있는 거리(m). 벽 안쪽이어야 한다")]
        [SerializeField] private float arenaReach = 7.4f;

        private readonly float[] cooldowns = new float[PandoraDefinition.SkillSlots];
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<SkillZone> zones = new List<SkillZone>();
        private float manaWait;
        // 몸짓 뒤에 나갈 기술. 없으면 null.
        private SkillDefinition pending;
        private int pendingSlot;
        private float pendingLeft;
        private Vector3 pendingAim;
        // 위상전이로 공격을 흘리면 대기가 줄어드는 칸. 없으면 -1.
        private int refundSlot = -1;
        // 마지막으로 연 차원이동 표시. 나온 뒤에도 그 프레임 끝까지는 남아 있다.
        private SkillGate gate;

        /// <summary>그 칸의 스킬을 썼다. 온라인에서 호스트가 참가자에게 알린다.</summary>
        public event System.Action<int> Used;

        public int Mana { get; private set; }
        public int MaxMana => fighter.Tuning.MaxMana;
        public PandoraDefinition Pandora => pandora;

        /// <summary>차원이동으로 숨어 나갈 곳을 고르는 중이면 그 표시, 아니면 null. 조작하는 쪽이 옮기고 나오게 한다.</summary>
        public SkillGate ActiveGate => gate != null && gate.IsOpen ? gate : null;

        public SkillDefinition SkillAt(int slot)
        {
            return pandora != null ? pandora.SkillAt(slot) : null;
        }

        /// <summary>
        /// 다른 판도라를 든다. 지금은 새 판을 시작하기 직전에만 부른다(대기와 마나는 판을 시작할 때 처음으로 돌아간다).
        /// 싸우는 중에 바꾸는 규칙(세 칸이 함께 3초 대기)은 판도라 교체를 만들 때 더한다.
        /// </summary>
        public void Equip(PandoraDefinition next)
        {
            pandora = next;
        }

        /// <summary>다시 쓸 수 있을 때까지 남은 비율(1이면 막 썼다, 0이면 쓸 수 있다).</summary>
        public float CooldownRatio(int slot)
        {
            SkillDefinition skill = SkillAt(slot);
            return skill != null && skill.Cooldown > 0f ? Mathf.Clamp01(cooldowns[slot] / skill.Cooldown) : 0f;
        }

        public bool HasManaFor(int slot)
        {
            SkillDefinition skill = SkillAt(slot);
            return skill != null && Mana >= skill.Cost;
        }

        public bool CanUse(int slot)
        {
            SkillDefinition skill = SkillAt(slot);
            if (skill == null || cooldowns[slot] > 0f || !HasManaFor(slot) || !fighter.CanAct)
            {
                return false;
            }

            // 차원참은 뒤로 넘어갈 상대가 겨냥 거리 안에 있어야 쓴다(2D판과 같다).
            return skill.Shape.Kind != SkillKind.Warp || TargetInAimRange();
        }

        /// <summary>이 인물이 깐 장판 가운데 지금 그 대상을 덮고 있는 것(AI가 빠져나갈 쪽을 고를 때 쓴다). 없으면 null.</summary>
        public SkillZone ZoneCovering(Fighter target)
        {
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                if (zones[i] == null)
                {
                    zones.RemoveAt(i);
                }
                else if (zones[i].IsCovering(target))
                {
                    return zones[i];
                }
            }

            return null;
        }

        /// <summary>그 칸의 스킬을 쓴다. 마나가 모자라거나, 대기 중이거나, 지금 움직일 수 없으면 false.</summary>
        public bool Use(int slot)
        {
            if (!CanUse(slot) || !Begin(slot))
            {
                return false;
            }

            Used?.Invoke(slot);
            return true;
        }

        /// <summary>
        /// 호스트에서 쓴 스킬을 참가자 화면에서 같은 몸짓·효과로 보인다. 쓸 수 있었는지는 호스트가 따졌다.
        /// 효과가 상대에게 닿아도 맞지 않는다(맞음은 호스트가 따로 알린다, <see cref="Fighter.ShowHostHit"/>).
        /// </summary>
        public void Replay(int slot)
        {
            if (SkillAt(slot) != null)
            {
                Begin(slot);
            }
        }

        /// <summary>호스트의 마나를 참가자 화면에 맞춘다(평타로 얻는 마나는 호스트가 맞힌 것만 센다).</summary>
        public void ShowHostMana(int mana)
        {
            Mana = mana;
        }

        private bool Begin(int slot)
        {
            SkillDefinition skill = SkillAt(slot);
            Vector3 aim = AimDirection();
            if (!fighter.BeginSkill(MoveFor(skill, aim)))
            {
                return false;
            }

            Mana = Mathf.Max(0, Mana - skill.Cost);
            cooldowns[slot] = skill.Cooldown;
            pending = skill;
            pendingSlot = slot;
            pendingLeft = skill.ReleaseAt;
            pendingAim = aim;
            return true;
        }

        private void OnEnable()
        {
            fighter.Landed += OnLanded;
            fighter.Restarted += OnRestarted;
            fighter.Evaded += OnEvaded;
        }

        private void OnDisable()
        {
            fighter.Landed -= OnLanded;
            fighter.Restarted -= OnRestarted;
            fighter.Evaded -= OnEvaded;
        }

        private void Start()
        {
            Mana = MaxMana;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int slot = 0; slot < cooldowns.Length; slot++)
            {
                cooldowns[slot] = Mathf.Max(0f, cooldowns[slot] - deltaTime);
            }

            RegenerateMana(deltaTime);
            if (pending != null)
            {
                pendingLeft -= deltaTime;
                if (pendingLeft <= 0f)
                {
                    Release();
                }
            }
        }

        private void RegenerateMana(float deltaTime)
        {
            if (fighter.IsDown)
            {
                return;
            }

            manaWait += deltaTime;
            while (manaWait >= fighter.Tuning.ManaRegenInterval)
            {
                manaWait -= fighter.Tuning.ManaRegenInterval;
                Mana = Mathf.Min(MaxMana, Mana + 1);
            }
        }

        private void OnLanded(Hit hit)
        {
            Mana = Mathf.Min(MaxMana, Mana + fighter.Tuning.ManaPerHit);
        }

        /// <summary>위상전이로 공격을 흘렸다: 흘린 자리에서 조각이 튀고, 위상전이의 대기가 한 번 줄어든다(2D판과 같다).</summary>
        private void OnEvaded()
        {
            if (refundSlot < 0)
            {
                return;
            }

            SkillDefinition phase = SkillAt(refundSlot);
            cooldowns[refundSlot] = Mathf.Max(0f, cooldowns[refundSlot] - phase.Shape.Refund);
            refundSlot = -1;
            if (phase.Impact != null)
            {
                Spawn(phase.Impact, transform.position + Vector3.up * 0.9f);
            }
        }

        private void OnRestarted()
        {
            Mana = MaxMana;
            manaWait = 0f;
            pending = null;
            refundSlot = -1;
            gate = null;
            System.Array.Clear(cooldowns, 0, cooldowns.Length);
            foreach (GameObject effect in spawned)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }

            spawned.Clear();
            zones.Clear();
        }

        /// <summary>겨냥할 쪽: 상대가 겨냥 거리 안에 있으면 상대, 아니면 바라보는 쪽.</summary>
        private Vector3 AimDirection()
        {
            return TargetInAimRange() ? GroundArea.Direction(transform.position, fighter.Opponent.transform.position, transform.forward) : transform.forward;
        }

        private bool TargetInAimRange()
        {
            Fighter target = fighter.Opponent;
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            return !target.IsDown && toTarget.sqrMagnitude <= fighter.Tuning.AimRange * fighter.Tuning.AimRange;
        }

        private SkillMove MoveFor(SkillDefinition skill, Vector3 aim)
        {
            SkillShape shape = skill.Shape;
            float flight = shape.FlightTime;
            switch (shape.Kind)
            {
                case SkillKind.Leap:
                    // 상대가 뛰는 거리보다 가까우면 상대 바로 앞에 내린다.
                    Vector3 toTarget = fighter.Opponent.transform.position - transform.position;
                    toTarget.y = 0f;
                    float distance = Mathf.Min(shape.Range, Mathf.Max(0f, toTarget.magnitude - LandingGap));
                    return new SkillMove(skill.Pose, skill.CastTime, aim, aim * distance, flight, flight + LandingGrace);
                case SkillKind.Dash:
                    // 피하는 도약이라 상대가 아니라 바라보는 쪽(조이스틱을 민 쪽)으로 정한 거리를 끝까지 내달린다(2D판과 같다).
                    // 그동안 맞지 않고, 길에 있는 상대 몸을 뚫고 지나간다.
                    Vector3 facing = transform.forward;
                    return new SkillMove(skill.Pose, skill.CastTime, facing, facing * shape.Range, flight, flight + LandingGrace, passThrough: true);
                case SkillKind.Warp:
                    // 사라지는 몸짓부터 나타나 벤 직후까지 맞지 않는다.
                    return new SkillMove(skill.Pose, skill.CastTime, aim, invulnerable: skill.ReleaseAt + WarpGrace);
                default:
                    return new SkillMove(skill.Pose, skill.CastTime, aim);
            }
        }

        /// <summary>몸짓의 정해진 시각: 기술을 내보낸다. 그 전에 맞아 몸짓이 끊겼으면 나가지 않는다.</summary>
        private void Release()
        {
            SkillDefinition skill = pending;
            pending = null;
            if (!fighter.IsUsingSkill)
            {
                return;
            }

            SkillShape shape = skill.Shape;
            Vector3 feet = transform.position;
            switch (shape.Kind)
            {
                case SkillKind.Projectile:
                case SkillKind.Grab:
                    // 몸은 쓸 때 겨냥한 쪽으로 돌아 있어서 CastPoint가 겨냥한 줄 위에 있다.
                    Spawn(skill.Effect, fighter.CastPoint).GetComponent<SkillProjectile>().Launch(fighter, skill, pendingAim);
                    break;
                case SkillKind.Zone:
                    SpawnZone(skill.Effect, feet).Begin(fighter, skill, shape.Radius, shape.Band, shape.GrowTime, shape.Follow);
                    break;
                case SkillKind.Leap:
                    Land(skill);
                    break;
                case SkillKind.Trap:
                    // 겨냥할 상대가 있으면 그 발밑, 없으면 바라보는 쪽 앞에 깐다.
                    Vector3 spot = TargetInAimRange() ? fighter.Opponent.transform.position : feet + pendingAim * shape.Range;
                    Spawn(skill.Effect, spot).GetComponent<SkillTrap>().Begin(fighter, skill);
                    break;
                case SkillKind.Thorns:
                    Spawn(skill.Effect, feet).GetComponent<SkillThorns>().Begin(fighter, skill);
                    break;
                case SkillKind.Phase:
                    fighter.Phase(shape.Duration);
                    refundSlot = pendingSlot;
                    GameObject aura = Spawn(skill.Effect, feet);
                    aura.transform.SetParent(transform, true);
                    Destroy(aura, shape.Duration);
                    break;
                case SkillKind.Dash:
                    Spawn(skill.Effect, feet + Vector3.up * 0.9f).GetComponent<SkillDash>().Begin(fighter, skill);
                    break;
                case SkillKind.Warp:
                    WarpBehind(skill);
                    break;
                case SkillKind.Gate:
                    gate = Spawn(skill.Effect, feet).GetComponent<SkillGate>();
                    gate.Begin(fighter, skill, arenaReach);
                    break;
            }
        }

        /// <summary>차원참: 상대 바로 뒤(상대와 내가 이은 줄의 건너편)로 순간이동해, 상대를 내가 있던 쪽으로 밀어 벤다.</summary>
        private void WarpBehind(SkillDefinition skill)
        {
            Fighter target = fighter.Opponent;
            Vector3 across = GroundArea.Direction(transform.position, target.transform.position, transform.forward);
            Vector3 behind = target.transform.position + across * (target.BodyRadius + skill.Shape.Size);
            behind = Vector3.ClampMagnitude(new Vector3(behind.x, 0f, behind.z), arenaReach) + Vector3.up * transform.position.y;
            Spawn(skill.Effect, transform.position);
            fighter.Blink(behind, -across);
            Spawn(skill.Effect, behind);
            target.TakeHit(fighter.RollHit(target, skill.Power, -across * skill.Shape.Knock, staggers: true));
            if (skill.Impact != null)
            {
                Spawn(skill.Impact, target.transform.position);
            }
        }

        /// <summary>도약에서 내린다: 둘레를 내리치고, 그 자리에 장판을 남긴다.</summary>
        private void Land(SkillDefinition skill)
        {
            SkillShape shape = skill.Shape;
            Vector3 feet = transform.position;
            if (skill.Impact != null)
            {
                Spawn(skill.Impact, feet);
            }

            Fighter target = fighter.Opponent;
            if (!target.IsDown && GroundArea.InCircle(feet, shape.Radius, target.transform.position, target.BodyRadius))
            {
                Vector3 away = GroundArea.Direction(feet, target.transform.position, transform.forward);
                target.TakeHit(fighter.RollHit(target, skill.Power, away * shape.Knock, staggers: true));
                if (skill.Burn > 0f)
                {
                    target.Burn(skill.Burn);
                }
            }

            SpawnZone(skill.Effect, feet).Begin(fighter, skill, shape.PuddleRadius, ringBand: 0f, spreadTime: 0f, followOwner: false);
        }

        private GameObject Spawn(GameObject prefab, Vector3 position)
        {
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            spawned.Add(instance);
            return instance;
        }

        private SkillZone SpawnZone(GameObject prefab, Vector3 position)
        {
            var zone = Spawn(prefab, position).GetComponent<SkillZone>();
            zones.Add(zone);
            return zone;
        }
    }
}
