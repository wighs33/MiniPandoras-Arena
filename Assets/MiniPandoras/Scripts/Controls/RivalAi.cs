using MiniPandoras.Fighters;
using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>
    /// 연습 상대를 조작하는 AI. 사람(<see cref="PlayerControls"/>)과 똑같이 Fighter의 Move·Attack과 SkillCaster의 Use만 부른다.
    /// 다가가기 → 3연격(몇 타까지 이을지는 확률) → 숨 고르기나 옆으로 돌기 → 다시 다가가기를 되풀이한다.
    /// 상대가 가까이서 공격을 시작하면 반응 시간 뒤에 알아채고 물러서기도 하고, 맞으면 반격하거나 물러선다.
    /// 치거나 물러서는 중이 아니면 가끔 거리에 맞는 스킬을 쓰고, 상대가 깐 장판 안에 서면 먼저 빠져나온다.
    /// </summary>
    public sealed class RivalAi : MonoBehaviour
    {
        private enum Plan
        {
            Approach,
            Strike,
            Wait,
            Circle,
            BackOff,
        }

        [SerializeField] private RivalAiTuning tuning;
        [SerializeField] private Fighter fighter;
        [SerializeField] private SkillCaster skills;
        [Tooltip("상대의 스킬. 상대가 깐 장판 안에 서면 먼저 빠져나온다")]
        [SerializeField] private SkillCaster opponentSkills;

        private Plan plan;
        private float skillWait;
        private float planLeft;
        private int pressesLeft;
        private float pressWait;
        private float circleSide;
        private bool targetWasAttacking;
        // 상대의 공격을 알아채기까지 남은 시간. 음수면 알아챌 것이 없다.
        private float noticeLeft = -1f;

        private Fighter Target => fighter.Opponent;

        private void OnEnable()
        {
            fighter.Struck += OnStruck;
            Begin(Plan.Approach, 0f);
        }

        private void OnDisable()
        {
            fighter.Struck -= OnStruck;
            fighter.Move(Vector3.zero);
        }

        private void Update()
        {
            if (fighter.IsDown || Target.IsDown)
            {
                fighter.Move(Vector3.zero);
                return;
            }

            // 차원이동으로 숨었으면 잠깐 뒤 상대 등 뒤로 나온다.
            SkillGate gate = skills != null ? skills.ActiveGate : null;
            if (gate != null)
            {
                if (gate.HiddenFor >= tuning.GateHideTime)
                {
                    Vector3 behind = Target.transform.position - Target.transform.forward * tuning.GateExitGap;
                    gate.PlaceAt(behind);
                    gate.Leave();
                }

                return;
            }

            // 상대가 숨었으면 나올 때까지 그 자리에서 기다린다.
            if (Target.IsHidden)
            {
                fighter.Move(Vector3.zero);
                return;
            }

            Vector3 toTarget = Target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 forward = distance > 0.001f ? toTarget / distance : transform.forward;
            float deltaTime = Time.deltaTime;

            planLeft -= deltaTime;
            WatchTarget(distance, deltaTime);
            // 휘두르는 중이 아니면 반격하려던 참이어도 장판부터 빠져나온다.
            if (!fighter.IsAttacking && EscapeZone())
            {
                return;
            }

            if (plan != Plan.Strike && plan != Plan.BackOff && TryUseSkill(distance, deltaTime))
            {
                return;
            }

            switch (plan)
            {
                case Plan.Approach:
                    Approach(forward, distance);
                    break;
                case Plan.Strike:
                    Strike(forward, deltaTime);
                    break;
                case Plan.Wait:
                    fighter.Move(Vector3.zero);
                    GoAgainWhenDone();
                    break;
                case Plan.Circle:
                    Circle(forward, distance);
                    GoAgainWhenDone();
                    break;
                case Plan.BackOff:
                    fighter.Move(-forward);
                    if (planLeft <= 0f)
                    {
                        BeginCircle();
                    }

                    break;
            }
        }

        private void Approach(Vector3 forward, float distance)
        {
            // 상대가 자기 장판 한가운데 서 있으면 들어가지 않고 밖에서 기다린다(들어갔다 빠져나오기를 되풀이하지 않게).
            // 따라다니는 장판은 기다려도 따라오므로 그대로 다가간다.
            SkillZone guard = opponentSkills != null ? opponentSkills.ZoneCovering(Target) : null;
            if (guard != null && !guard.FollowsOwner)
            {
                fighter.Move(Vector3.zero);
                return;
            }

            if (distance <= tuning.AttackRange)
            {
                BeginStrike(firstPressDelay: 0f);
                return;
            }

            fighter.Move(forward);
        }

        private void Strike(Vector3 forward, float deltaTime)
        {
            // 조이스틱을 상대 쪽으로 두면 다음 타가 상대를 보고 나간다(공격 중에는 걷지 않는다).
            fighter.Move(forward * 0.3f);
            if (pressesLeft > 0)
            {
                pressWait -= deltaTime;
                if (pressWait <= 0f && fighter.CanAct)
                {
                    fighter.Attack();
                    pressesLeft--;
                    pressWait = tuning.PressInterval;
                }

                return;
            }

            if (!fighter.IsAttacking)
            {
                AfterStrike();
            }
        }

        /// <summary>상대 둘레를 옆으로 돌며 거리를 지킨다.</summary>
        private void Circle(Vector3 forward, float distance)
        {
            Vector3 side = Vector3.Cross(Vector3.up, forward) * circleSide;
            float closer = Mathf.Clamp(distance - tuning.CircleDistance, -0.5f, 0.5f);
            fighter.Move((side + forward * closer).normalized * tuning.CircleStrength);
        }

        /// <summary>상대가 공격을 시작한 순간을 반응 시간 뒤에 알아채고, 가까우면 확률로 물러선다.</summary>
        private void WatchTarget(float distance, float deltaTime)
        {
            bool attacking = Target.IsAttacking;
            if (attacking && !targetWasAttacking)
            {
                noticeLeft = tuning.ReactionTime;
            }

            targetWasAttacking = attacking;
            if (noticeLeft < 0f)
            {
                return;
            }

            noticeLeft -= deltaTime;
            if (noticeLeft > 0f)
            {
                return;
            }

            noticeLeft = -1f;
            // 치는 중이면 피하지 않고 맞받아친다.
            if (plan != Plan.Strike && distance < tuning.BackOffDistance && Random.value < tuning.BackOffChance)
            {
                Begin(Plan.BackOff, tuning.BackOffTime);
            }
        }

        /// <summary>
        /// 상대가 깐 장판 안이면 가장 빨리 벗어나는 쪽으로 달린다. 빠져나오는 중이면 true.
        /// 상대를 따라다니는 장판(검은 날개)은 도망쳐도 따라오므로 피하지 않고 맞서 싸운다.
        /// </summary>
        private bool EscapeZone()
        {
            SkillZone zone = opponentSkills != null ? opponentSkills.ZoneCovering(fighter) : null;
            if (zone == null || zone.FollowsOwner)
            {
                return false;
            }

            fighter.Move(zone.WayOut(transform.position));
            return true;
        }

        /// <summary>
        /// 가끔 스킬을 따진다: 쓸 수 있고 지금 거리가 그 스킬의 AI 거리 안에 드는 것을 찾아 쓴다.
        /// 찾는 순서는 매번 다른 칸에서 시작한다(늘 궁극기부터 찾으면 판이 시작하자마자 궁극기가 나갔다).
        /// 쓰면 그 몸짓 동안 쉬었다가 다시 다가간다.
        /// </summary>
        private bool TryUseSkill(float distance, float deltaTime)
        {
            skillWait -= deltaTime;
            if (skills == null || skillWait > 0f)
            {
                return false;
            }

            skillWait = tuning.SkillThinkInterval;
            if (Random.value >= tuning.SkillChance)
            {
                return false;
            }

            int first = Random.Range(0, PandoraDefinition.SkillSlots);
            for (int i = 0; i < PandoraDefinition.SkillSlots; i++)
            {
                int slot = (first + i) % PandoraDefinition.SkillSlots;
                SkillDefinition skill = skills.SkillAt(slot);
                bool inRange = skill != null && distance >= skill.AiRange.x && distance <= skill.AiRange.y;
                if (inRange && skills.Use(slot))
                {
                    fighter.Move(Vector3.zero);
                    Begin(Plan.Wait, skill.CastTime);
                    return true;
                }
            }

            return false;
        }

        private void OnStruck(Hit hit)
        {
            if (!hit.Staggers)
            {
                // 장판·화상처럼 휘청이지 않는 피해에는 하던 것을 그대로 한다.
                return;
            }

            if (Random.value < tuning.CounterChance)
            {
                // 휘청임이 끝나야 버튼이 먹히고, 그것을 알아채는 데 반응 시간이 걸린다.
                BeginStrike(firstPressDelay: tuning.ReactionTime);
            }
            else
            {
                Begin(Plan.BackOff, tuning.BackOffTime);
            }
        }

        private void BeginStrike(float firstPressDelay)
        {
            pressesLeft = 1;
            if (Random.value < tuning.SecondHitChance)
            {
                pressesLeft = Random.value < tuning.ThirdHitChance ? 3 : 2;
            }

            pressWait = firstPressDelay;
            Begin(Plan.Strike, 0f);
        }

        private void AfterStrike()
        {
            if (Random.value < tuning.CircleChance)
            {
                BeginCircle();
            }
            else
            {
                Begin(Plan.Wait, Random.Range(tuning.WaitTime.x, tuning.WaitTime.y));
            }
        }

        private void BeginCircle()
        {
            circleSide = Random.value < 0.5f ? -1f : 1f;
            Begin(Plan.Circle, Random.Range(tuning.CircleTime.x, tuning.CircleTime.y));
        }

        private void GoAgainWhenDone()
        {
            if (planLeft <= 0f)
            {
                Begin(Plan.Approach, 0f);
            }
        }

        private void Begin(Plan next, float seconds)
        {
            plan = next;
            planLeft = seconds;
        }
    }
}
