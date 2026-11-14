using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 인물의 Animator와 이야기하는 한 곳. 상태와 변수 이름은 여기에만 두고 Art/Animation/Fighter.controller와 맞춘다.
    /// 공격·피격 상태는 언제든 끊고 들어가야 해서 전이선 대신 코드에서 CrossFade로 부른다.
    /// </summary>
    public sealed class FighterAnimation : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
        private static readonly int HurtState = Animator.StringToHash("Hurt");
        private static readonly int HurtHeavyState = Animator.StringToHash("HurtHeavy");
        private static readonly int DownState = Animator.StringToHash("Down");
        // SkillPose 순서와 같다.
        private static readonly int[] SkillStates =
        {
            Animator.StringToHash("SkillShoot"),
            Animator.StringToHash("SkillSlam"),
            Animator.StringToHash("SkillLeap"),
            Animator.StringToHash("SkillDash"),
            Animator.StringToHash("SkillSlash"),
            Animator.StringToHash("SkillBurst"),
            Animator.StringToHash("SkillGuard"),
        };
        private static readonly int[] AttackStates =
        {
            Animator.StringToHash("Attack1"),
            Animator.StringToHash("Attack2"),
            Animator.StringToHash("Attack3"),
        };

        [SerializeField] private Animator animator;
        [Tooltip("이동 빠르기를 걷기·달리기 섞기에 넘길 때 부드럽게 따라가는 시간(초)")]
        [SerializeField] private float speedDamping = 0.06f;
        [Tooltip("공격 타로 넘어가는 섞기 시간(초)")]
        [SerializeField] private float attackBlend = 0.05f;
        [Tooltip("맞아서 휘청이는 동작으로 넘어가는 섞기 시간(초). 짧아야 맞은 순간이 또렷하다")]
        [SerializeField] private float hurtBlend = 0.04f;
        [Tooltip("공격이나 휘청임이 끝나 걷기로 돌아가는 섞기 시간(초)")]
        [SerializeField] private float returnBlend = 0.2f;

        public void SetMoveSpeed(float metersPerSecond)
        {
            animator.SetFloat(SpeedParameter, metersPerSecond, speedDamping, Time.deltaTime);
        }

        public void PlayAttack(int step)
        {
            animator.CrossFadeInFixedTime(AttackStates[step], attackBlend);
        }

        public void PlaySkill(SkillPose pose)
        {
            animator.CrossFadeInFixedTime(SkillStates[(int)pose], attackBlend, 0, 0f);
        }

        /// <param name="heavy">3연격의 마지막 타처럼 크게 밀려나는 타격</param>
        public void PlayHurt(bool heavy)
        {
            // 같은 상태를 연달아 맞아도 처음부터 다시 휘청이게 시각 0부터 튼다.
            animator.CrossFadeInFixedTime(heavy ? HurtHeavyState : HurtState, hurtBlend, 0, 0f);
        }

        public void PlayDown()
        {
            animator.CrossFadeInFixedTime(DownState, hurtBlend);
        }

        public void ReturnToLocomotion()
        {
            animator.CrossFadeInFixedTime(LocomotionState, returnBlend);
        }

        /// <summary>새 판: 섞지 않고 곧바로 선 자세로 돌아간다.</summary>
        public void Restart()
        {
            animator.speed = 1f;
            animator.SetFloat(SpeedParameter, 0f);
            animator.Play(LocomotionState, 0, 0f);
        }

        /// <summary>타격 정지 동안 동작을 그 자리에 멈춘다.</summary>
        public void SetFrozen(bool frozen)
        {
            animator.speed = frozen ? 0f : 1f;
        }
    }
}
