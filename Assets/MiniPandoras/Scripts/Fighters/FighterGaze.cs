using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 고개를 돌려 상대를 본다(Animator 시선 IK). 상대가 몸 옆·뒤에 있거나, 숨었거나, 쓰러졌으면 몸 앞 눈높이를 본다.
    /// 앞을 볼 때도 고개를 드는 것은, 전투 대기 자세가 고개를 숙이고 있어서 내려다보는 쿼터뷰에서는 얼굴이 가려지기 때문이다.
    /// 공격·피격·쓰러짐·스킬 모션은 몸이 크게 움직여서 시선을 붙잡으면 목이 꺾여 보이므로, 「BigMove」 꼬리표가 붙은 상태에서는 서서히 놓는다.
    /// 쏘는 몸짓(「Aim」 꼬리표)은 몸이 덜 움직여서 그대로 상대를 본다(겨냥하는 눈길).
    /// OnAnimatorIK는 Animator가 있는 오브젝트에만 오므로 모델 오브젝트에 붙인다(컨트롤러의 IK Pass가 켜져 있어야 한다).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class FighterGaze : MonoBehaviour
    {
        private const string BigMoveTag = "BigMove";

        [SerializeField] private Animator animator;
        [SerializeField] private Fighter fighter;
        [Tooltip("머리가 시선을 따르는 정도(0~1)")]
        [SerializeField, Range(0f, 1f)] private float headWeight = 0.85f;
        [Tooltip("몸통이 함께 도는 정도(0~1)")]
        [SerializeField, Range(0f, 1f)] private float bodyWeight = 0.1f;
        [Tooltip("상대를 보지 않을 때 몸 앞으로 이만큼 떨어진 곳을 본다(m)")]
        [SerializeField] private float lookDistance = 4f;
        [Tooltip("보는 점의 높이(m, 발 기준). 상대를 볼 때는 상대의 얼굴 높이다")]
        [SerializeField] private float lookHeight = 1.2f;
        [Tooltip("상대가 몸 앞에서 이 각도(도) 안에 있을 때만 상대를 본다. 더 옆이면 목이 너무 돌아가 보인다")]
        [SerializeField] private float opponentAngle = 70f;
        [Tooltip("보는 점이 새 곳으로 옮겨 가는 데 걸리는 시간(초). 상대가 앞을 지나가도 고개가 튀지 않게 한다")]
        [SerializeField] private float shiftTime = 0.15f;
        [Tooltip("큰 동작이 시작되고 끝날 때 시선을 놓고 잡는 데 걸리는 시간(초)")]
        [SerializeField] private float fadeTime = 0.12f;

        private float weight = 1f;
        // 몸에서 본 보는 점. 몸을 기준으로 두어야 내달리는 동안 보는 점이 뒤에 처지지 않는다.
        private Vector3 lookOffset;

        private void Awake()
        {
            lookOffset = AheadOffset();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            float target = IsMovingBig() ? 0f : 1f;
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime / fadeTime);
            lookOffset = Vector3.Lerp(lookOffset, TargetOffset(), Time.deltaTime / shiftTime);
            animator.SetLookAtWeight(weight, bodyWeight, headWeight, 0f, 0.5f);
            animator.SetLookAtPosition(transform.position + lookOffset);
        }

        private Vector3 TargetOffset()
        {
            Fighter opponent = fighter.Opponent;
            Vector3 toOpponent = opponent.transform.position - transform.position;
            toOpponent.y = 0f;
            bool canWatch = !opponent.IsDown && !opponent.IsHidden && Vector3.Angle(transform.forward, toOpponent) <= opponentAngle;
            return canWatch ? toOpponent + Vector3.up * lookHeight : AheadOffset();
        }

        private Vector3 AheadOffset()
        {
            return transform.forward * lookDistance + Vector3.up * lookHeight;
        }

        private bool IsMovingBig()
        {
            if (animator.IsInTransition(0))
            {
                return animator.GetNextAnimatorStateInfo(0).IsTag(BigMoveTag);
            }

            return animator.GetCurrentAnimatorStateInfo(0).IsTag(BigMoveTag);
        }
    }
}
