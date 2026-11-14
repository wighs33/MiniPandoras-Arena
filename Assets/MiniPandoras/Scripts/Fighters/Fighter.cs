using System;
using MiniPandoras.Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 대전 인물 한 명. 조작하는 쪽(사람·AI·네트워크)은 <see cref="Move"/>와 <see cref="Attack"/>을 부르고,
    /// 스킬 쪽은 <see cref="BeginSkill"/>로 몸을 빌린다. 지금 움직일 수 있는지, 공격이 어디까지 왔는지,
    /// 맞았을 때 어떻게 되는지는 여기서 정한다.
    /// </summary>
    public sealed class Fighter : MonoBehaviour
    {
        // 쏘는 기술이 나오는 자리(m): 발에서의 높이, 몸 앞으로의 거리.
        private const float CastHeight = 0.9f;
        private const float CastReach = 0.6f;

        [SerializeField] private FighterTuning tuning;
        [SerializeField] private FighterMotor motor;
        [SerializeField] private FighterAnimation animations;
        [SerializeField] private FighterFlash flash;
        [Tooltip("1대1의 상대. 3연격은 이 인물에게만 닿는다")]
        [SerializeField] private Fighter opponent;
        [Tooltip("숨을 때(차원이동) 감추는 그림(모델, 발밑 고리)")]
        [SerializeField] private GameObject[] visuals;

        private readonly BurnTimer burning = new BurnTimer();
        private ComboChain combo;
        private Vector3 moveIntent;
        private float stunLeft;
        private float frozenLeft;
        private Vector3 knockVelocity;
        private float knockLeft;
        private float skillLeft;
        private Vector3 travelVelocity;
        private float travelLeft;
        private float invulnerableLeft;
        private float slowLeft;
        private float phaseLeft;
        private bool hidden;
        private bool passingThrough;
        // 온라인 참가자 화면의 인물: 호스트가 한 일을 따라 보일 뿐 스스로 판정하지 않는다(FollowHost).
        private bool followsHost;
        // 맞는지는 호스트가 정한다(참가자 화면의 두 인물: 따라 하는 상대, 예측하는 나).
        private bool judgedByHost;

        /// <summary>맞았다. 화면(체력 숫자, 카메라 흔들림)이 듣는다.</summary>
        public event Action<Hit> Struck;

        /// <summary>공격 버튼을 받았다(3연격을 시작·이었거나 다음 타를 미리 눌러 뒀다). 온라인에서 호스트가 참가자에게 알린다.</summary>
        public event Action Attacked;

        /// <summary>3연격으로 상대를 맞혔다.</summary>
        public event Action<Hit> Landed;

        /// <summary>체력이 0이 되어 쓰러졌다.</summary>
        public event Action KnockedOut;

        /// <summary>새 판을 위해 다시 섰다. 인물에 딸린 것(스킬의 마나·장판)도 함께 처음으로 돌린다.</summary>
        public event Action Restarted;

        /// <summary>위상전이 중이라 공격을 흘려보냈다(맞지 않았다).</summary>
        public event Action Evaded;

        public int Hp { get; private set; }
        public int MaxHp => tuning.MaxHp;
        public bool IsDown => Hp == 0;
        public bool IsAttacking => combo.IsAttacking;
        public bool IsUsingSkill => skillLeft > 0f;

        /// <summary>뛰어오르거나 숨은 동안처럼 맞지 않는 때. 탄은 이때 지나간다.</summary>
        public bool IsInvulnerable => invulnerableLeft > 0f || hidden;

        /// <summary>차원이동으로 숨어 있다(보이지 않고, 맞지 않고, 움직이지 않는다).</summary>
        public bool IsHidden => hidden;

        /// <summary>지금 공격이나 스킬을 시작할 수 있는지(쓰러지지도, 휘청이지도, 스킬을 쓰는 중도, 숨은 중도 아님).</summary>
        public bool CanAct => !IsDown && stunLeft <= 0f && skillLeft <= 0f && !hidden;

        /// <summary>
        /// 쏘는 기술(탄, 뻗는 손)이 나오는 자리: 몸 앞 가슴 높이. 그림(손 뼈)이 아니라 몸의 자리로 정해서 온라인에서도 모두 같고,
        /// 손이 IK로 이곳에 뻗는다(<see cref="FighterAim"/>).
        /// </summary>
        public Vector3 CastPoint => transform.position + Vector3.up * CastHeight + transform.forward * CastReach;
        public Fighter Opponent => opponent;
        public FighterTuning Tuning => tuning;
        public float BodyRadius => motor.Radius;

        /// <summary>바닥을 따라 움직이는 빠르기(m/s).</summary>
        public float Speed => motor.Speed;

        /// <summary>가고 싶은 방향(월드, 길이 0~1). 공격 중에는 다음 타를 칠 방향이 된다.</summary>
        public void Move(Vector3 direction)
        {
            moveIntent = Vector3.ClampMagnitude(direction, 1f);
        }

        /// <summary>
        /// 온라인 참가자 화면의 인물로 바꾼다(되돌리면 false). 몸은 호스트가 알려 준 자리에 두고(<see cref="Follow"/>),
        /// 이 화면에서 계산한 맞히기는 받지 않고 호스트가 알려 준 맞음(<see cref="ShowHostHit"/>)만 받는다.
        /// 공격·스킬은 호스트가 받아들인 것만 오므로 지금 할 수 있는지 다시 따지지 않는다.
        /// </summary>
        public void FollowHost(bool follow)
        {
            followsHost = follow;
            judgedByHost = follow;
            motor.FollowHost(follow);
        }

        /// <summary>
        /// 온라인 참가자 화면의 내 인물로 바꾼다(되돌리면 false). 조작에 바로 움직이고 몸짓도 바로 하지만(예측),
        /// 맞는지는 호스트가 정한다(<see cref="ShowHostHit"/>만 받는다). 자리는 <see cref="Nudge"/>로 호스트에 맞춰 간다.
        /// </summary>
        public void PredictForHost(bool predict)
        {
            judgedByHost = predict;
        }

        /// <summary>호스트가 알려 준 자리·방향(바닥에서 돈 각도)·빠르기로 몸을 둔다.</summary>
        public void Follow(Vector3 position, float yaw, float speed)
        {
            motor.Place(position, yaw, speed);
        }

        /// <summary>예측한 자리를 호스트의 자리 쪽으로 그만큼 옮긴다.</summary>
        public void Nudge(Vector3 offset)
        {
            motor.Nudge(offset);
        }

        /// <summary>공격 버튼. 누를 때마다 3연격의 다음 타로 이어진다. 휘청이거나 스킬을 쓰는 동안에는 듣지 않는다.</summary>
        public void Attack()
        {
            if (!CanAct && !followsHost)
            {
                return;
            }

            Attacked?.Invoke();
            if (combo.Press())
            {
                BeginStep();
            }
        }

        /// <summary>스킬을 쓰는 동안 몸을 맡긴다. 하던 3연격은 끊는다. 지금 쓸 수 없으면 false.</summary>
        public bool BeginSkill(SkillMove move)
        {
            if (!CanAct && !followsHost)
            {
                return false;
            }

            combo.Cancel();
            if (move.Facing.sqrMagnitude > 0.0001f)
            {
                motor.Face(move.Facing);
            }

            skillLeft = move.Duration;
            travelLeft = move.TravelTime;
            travelVelocity = move.TravelTime > 0f ? move.Travel / move.TravelTime : Vector3.zero;
            invulnerableLeft = Mathf.Max(invulnerableLeft, move.Invulnerable);
            SetPassingThrough(move.PassThrough);
            animations.PlaySkill(move.Pose);
            return true;
        }

        /// <summary>그 시간(초) 동안 들어오는 공격을 모두 흘려보낸다(위상전이). 움직이고 싸우는 것은 그대로 할 수 있다.</summary>
        public void Phase(float seconds)
        {
            phaseLeft = Mathf.Max(phaseLeft, seconds);
        }

        /// <summary>그 자리로 순간이동해 그쪽을 본다(차원참).</summary>
        public void Blink(Vector3 position, Vector3 facing)
        {
            motor.Teleport(position, Quaternion.LookRotation(facing));
        }

        /// <summary>틈으로 숨는다(차원이동): 보이지 않고, 맞지 않고, 상대가 부딪히지도 않는다.</summary>
        public void Hide()
        {
            hidden = true;
            combo.Cancel();
            skillLeft = 0f;
            moveIntent = Vector3.zero;
            SetVisible(false);
            motor.SetSolid(false);
        }

        /// <summary>숨었던 틈에서 그 자리로 나와 상대를 본다. 나온 뒤 잠깐 맞지 않는다.</summary>
        public void Reveal(Vector3 position, float safeTime)
        {
            Vector3 toOpponent = opponent.transform.position - position;
            toOpponent.y = 0f;
            Quaternion facing = toOpponent.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toOpponent) : transform.rotation;
            motor.Teleport(position, facing);
            SetVisible(true);
            hidden = false;
            invulnerableLeft = Mathf.Max(invulnerableLeft, safeTime);
        }

        /// <summary>그 대상에게 줄 피해를 2D판 공식으로 굴린다(치명타 포함). 3연격과 스킬이 함께 쓴다.</summary>
        public Hit RollHit(Fighter target, float power, Vector3 push, bool staggers, bool finisher = false)
        {
            bool critical = Random.value < tuning.CriticalChance;
            int damage = DamageFormula.Compute(tuning.Attack, target.tuning.Defense, power, Random.value);
            if (critical)
            {
                damage = DamageFormula.Critical(damage);
            }

            return new Hit(damage, critical, finisher, push, transform.position, staggers);
        }

        /// <summary>
        /// 맞는다. 피해와 밀려남은 때린 쪽(상대의 3연격, 스킬, 나중에는 호스트)이 정해서 넘긴다.
        /// 뛰어오른 동안처럼 맞지 않는 때에는 아무 일도 없다.
        /// </summary>
        public void TakeHit(Hit hit)
        {
            // 참가자 화면에서 그려 본 공격이 닿아도 맞지 않는다. 맞았는지는 호스트가 정해 ShowHostHit으로 알린다.
            if (judgedByHost || IsDown || IsInvulnerable)
            {
                return;
            }

            if (phaseLeft > 0f)
            {
                Evaded?.Invoke();
                return;
            }

            Suffer(hit);
        }

        /// <summary>호스트에서 맞은 것을 참가자 화면에서 그대로 맞는다(체력, 휘청임, 숫자). 맞을 수 있었는지는 호스트가 따졌다.</summary>
        public void ShowHostHit(Hit hit)
        {
            Suffer(hit);
        }

        /// <summary>호스트에서 위상전이로 공격을 흘린 것을 참가자 화면에 보인다.</summary>
        public void ShowHostEvade()
        {
            Evaded?.Invoke();
        }

        private void Suffer(Hit hit)
        {
            Hp = Mathf.Max(0, Hp - hit.Damage);
            flash.Play();
            if (hit.Staggers || IsDown)
            {
                Stagger(hit);
            }

            Struck?.Invoke(hit);
            if (IsDown)
            {
                KnockedOut?.Invoke();
            }
        }

        /// <summary>불이 붙는다: 최대 체력의 그 비율만큼을 정해진 간격으로 몇 번 깎는다(2D판의 dot).</summary>
        public void Burn(float maxHpRatio)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(tuning.MaxHp * maxHpRatio));
            burning.Ignite(damage, tuning.BurnTicks, tuning.BurnInterval);
        }

        /// <summary>그 시간(초) 동안 걸음이 느려진다(2D판의 slow). 더 긴 쪽이 남는다.</summary>
        public void Slow(float seconds)
        {
            slowLeft = Mathf.Max(slowLeft, seconds);
        }

        /// <summary>새 판: 체력을 채우고, 하던 공격·스킬·휘청임·밀려남·화상·느려짐을 모두 잊고, 그 자리에 선다.</summary>
        public void Restart(Vector3 position, Quaternion facing)
        {
            Hp = tuning.MaxHp;
            combo.Cancel();
            burning.Clear();
            moveIntent = Vector3.zero;
            stunLeft = 0f;
            frozenLeft = 0f;
            knockLeft = 0f;
            skillLeft = 0f;
            travelLeft = 0f;
            invulnerableLeft = 0f;
            slowLeft = 0f;
            phaseLeft = 0f;
            hidden = false;
            SetPassingThrough(false);
            SetVisible(true);
            motor.Teleport(position, facing);
            animations.Restart();
            Restarted?.Invoke();
        }

        private void Awake()
        {
            combo = new ComboChain(tuning.Combo);
            Hp = tuning.MaxHp;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            invulnerableLeft -= deltaTime;
            slowLeft -= deltaTime;
            phaseLeft -= deltaTime;
            if (hidden)
            {
                return;
            }
            if (frozenLeft > 0f)
            {
                frozenLeft -= deltaTime;
                if (frozenLeft > 0f)
                {
                    return;
                }

                animations.SetFrozen(false);
            }

            int burn = burning.Tick(deltaTime);
            if (burn > 0)
            {
                TakeHit(new Hit(burn, false, false, Vector3.zero, transform.position, staggers: false));
            }

            if (IsDown || stunLeft > 0f)
            {
                Reel(deltaTime);
            }
            else if (skillLeft > 0f)
            {
                ContinueSkill(deltaTime);
            }
            else
            {
                Act(deltaTime);
            }
        }

        private void Act(float deltaTime)
        {
            switch (combo.Tick(deltaTime))
            {
                case ComboChain.Change.Struck:
                    Strike();
                    break;
                case ComboChain.Change.StepStarted:
                    BeginStep();
                    break;
                case ComboChain.Change.Finished:
                    animations.ReturnToLocomotion();
                    break;
            }

            if (combo.IsAttacking)
            {
                motor.Lunge(LungeSpeed(), deltaTime);
            }
            else
            {
                motor.Run(slowLeft > 0f ? moveIntent * tuning.SlowFactor : moveIntent, deltaTime);
            }

            animations.SetMoveSpeed(combo.IsAttacking ? 0f : motor.Speed);
        }

        /// <summary>스킬 몸짓을 하는 동안: 미끄러질 것이 남았으면 미끄러지고, 끝나면 다시 걷는다.</summary>
        private void ContinueSkill(float deltaTime)
        {
            skillLeft -= deltaTime;
            motor.Slide(travelVelocity * ShareOf(ref travelLeft, deltaTime), deltaTime);
            if (travelLeft <= 0f)
            {
                SetPassingThrough(false);
            }
            animations.SetMoveSpeed(0f);
            if (skillLeft <= 0f)
            {
                animations.ReturnToLocomotion();
            }
        }

        /// <summary>맞아서 밀려나고 휘청인다. 휘청임이 끝나면 다시 걷는다(쓰러졌으면 누운 채로 둔다).</summary>
        private void Reel(float deltaTime)
        {
            motor.Slide(knockVelocity * ShareOf(ref knockLeft, deltaTime), deltaTime);
            animations.SetMoveSpeed(0f);
            if (IsDown)
            {
                return;
            }

            stunLeft -= deltaTime;
            if (stunLeft <= 0f)
            {
                animations.ReturnToLocomotion();
            }
        }

        /// <summary>하던 공격·스킬을 놓치고 휘청이며 밀려난다(체력이 다했으면 쓰러진다).</summary>
        private void Stagger(Hit hit)
        {
            combo.Cancel();
            skillLeft = 0f;
            travelLeft = 0f;
            SetPassingThrough(false);
            stunLeft = tuning.HitStun;
            knockLeft = tuning.KnockTime;
            knockVelocity = hit.Push / tuning.KnockTime;
            Vector3 toSource = hit.Source - transform.position;
            toSource.y = 0f;
            if (toSource.sqrMagnitude > 0.0001f)
            {
                // 때린 쪽을 보고 휘청여야 맞은 방향이 읽힌다.
                motor.Face(toSource);
            }

            if (IsDown)
            {
                animations.PlayDown();
            }
            else
            {
                animations.PlayHurt(hit.Finisher);
            }

            Freeze(StopTime(hit));
        }

        private void BeginStep()
        {
            // 치기 직전에 조이스틱 쪽으로 돌아선다. 연타하면서 방향을 바꿀 수 있다.
            if (moveIntent.sqrMagnitude > 0.04f)
            {
                motor.Face(moveIntent);
            }

            animations.PlayAttack(combo.Step);
        }

        /// <summary>지금 타의 때리는 시각: 몸 앞의 원 안에 상대가 있으면 맞힌다.</summary>
        private void Strike()
        {
            ComboStep step = combo.Current;
            Vector3 center = transform.position + transform.forward * step.Reach;
            Vector3 target = opponent.transform.position;
            if (opponent.IsDown || !GroundArea.InCircle(center, step.Radius, target, opponent.BodyRadius))
            {
                return;
            }

            Vector3 away = GroundArea.Direction(transform.position, target, transform.forward);
            Hit hit = RollHit(opponent, step.Power, away * step.Knock, staggers: true, finisher: combo.IsFinisher);
            opponent.TakeHit(hit);
            Freeze(StopTime(hit));
            Landed?.Invoke(hit);
        }

        /// <summary>공간도약처럼 상대 몸을 뚫고 지나가는 동안은 두 몸이 부딪히지 않게 한다.</summary>
        private void SetPassingThrough(bool pass)
        {
            if (passingThrough == pass)
            {
                return;
            }

            passingThrough = pass;
            motor.PassThrough(opponent.motor, pass);
        }

        private void SetVisible(bool visible)
        {
            foreach (GameObject part in visuals)
            {
                part.SetActive(visible);
            }
        }

        /// <summary>
        /// 남은 시간(밀려남·미끄러짐) 가운데 이번 프레임이 차지하는 몫(0~1)을 돌려주고 남은 시간을 줄인다.
        /// 마지막 프레임에는 남은 만큼만 움직여서, 프레임 길이와 상관없이 정확히 정한 거리만큼 간다.
        /// </summary>
        private static float ShareOf(ref float timeLeft, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            float share = Mathf.Clamp01(timeLeft / deltaTime);
            timeLeft -= deltaTime;
            return share;
        }

        /// <summary>타격 정지: 때린 쪽과 맞은 쪽이 잠깐 함께 멈춰 맞은 순간을 느끼게 한다.</summary>
        private void Freeze(float seconds)
        {
            frozenLeft = Mathf.Max(frozenLeft, seconds);
            animations.SetFrozen(true);
        }

        private float StopTime(Hit hit)
        {
            return hit.Critical ? tuning.CriticalHitStop : tuning.HitStop;
        }

        private float LungeSpeed()
        {
            ComboStep step = combo.Current;
            if (combo.Elapsed >= step.LungeTime || IsRightInFrontOfOpponent())
            {
                return 0f;
            }

            return step.Lunge / step.LungeTime;
        }

        /// <summary>상대가 바로 앞에 붙어 있는지. 그러면 더 내딛지 않는다(파고들면 두 몸이 겹쳐 보인다).</summary>
        private bool IsRightInFrontOfOpponent()
        {
            Vector3 toOpponent = opponent.transform.position - transform.position;
            toOpponent.y = 0f;
            float stop = tuning.LungeStopDistance;
            return toOpponent.sqrMagnitude <= stop * stop && Vector3.Dot(toOpponent, transform.forward) > 0f;
        }
    }
}
