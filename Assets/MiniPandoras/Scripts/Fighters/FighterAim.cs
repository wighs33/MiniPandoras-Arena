using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 쏘는 몸짓(「Aim」 꼬리표가 붙은 상태) 동안 왼손을 <see cref="Fighter.CastPoint"/>로 뻗는다(Animator 손 IK).
    /// 탄은 그림과 상관없이 몸 앞 정해진 자리에서 나가므로(온라인에서 모두 같게), 손이 그 자리로 가야 손바닥에서 나가는 것처럼 보인다.
    /// 몸은 쓸 때 겨냥한 쪽으로 돌아 있어서, 손은 겨냥한 상대를 가리킨다.
    /// OnAnimatorIK는 Animator가 있는 오브젝트에만 오므로 모델 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class FighterAim : MonoBehaviour
    {
        private const string AimTag = "Aim";

        [SerializeField] private Animator animator;
        [SerializeField] private Fighter fighter;
        [Tooltip("손을 뻗고 거두는 데 걸리는 시간(초). 쏘는 시각(0.2초 안팎)보다 짧아야 손이 먼저 닿는다")]
        [SerializeField] private float reachTime = 0.1f;

        private float weight;

        private void OnAnimatorIK(int layerIndex)
        {
            float target = IsAiming() ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime / reachTime);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, fighter.CastPoint);
        }

        private bool IsAiming()
        {
            if (animator.IsInTransition(0))
            {
                return animator.GetNextAnimatorStateInfo(0).IsTag(AimTag);
            }

            return animator.GetCurrentAnimatorStateInfo(0).IsTag(AimTag);
        }
    }
}
