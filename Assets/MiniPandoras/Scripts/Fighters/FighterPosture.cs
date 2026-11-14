using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 리타깃된 자세에서 뒤로 젖혀진 몸을 바로 세운다. Luna는 몸통이 짧고 척추 관절이 몸 앞쪽에 있어서, 사람 비율 모션을 입히면
    /// 유니티가 몸 전체 방향을 맞추느라 골반을 모션 원본보다 5~9° 뒤로 젖힌다(같은 클립을 원본 뼈대와 나란히 재서 확인했다.
    /// Docs/ARCHITECTURE.md의 「몸 기울기」). 몸을 그만큼 앞으로 숙이고, 발은 IK로 모션의 제자리에 둔다.
    /// OnAnimatorIK는 Animator가 있는 오브젝트에만 오므로 모델 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class FighterPosture : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("몸을 앞으로 숙이는 각도(도). 골반 기울기가 모션 원본과 같아지게 잰 값이다")]
        [SerializeField] private float forwardLean = 7.5f;

        private void OnAnimatorIK(int layerIndex)
        {
            // 몸을 돌리기 전에 발의 자리를 붙잡아 둔다. 그러지 않으면 다리까지 함께 돌아 발이 땅에서 뜨거나 묻힌다.
            Pin(AvatarIKGoal.LeftFoot);
            Pin(AvatarIKGoal.RightFoot);
            animator.bodyRotation *= Quaternion.AngleAxis(forwardLean, Vector3.right);
        }

        private void Pin(AvatarIKGoal foot)
        {
            animator.SetIKPositionWeight(foot, 1f);
            animator.SetIKRotationWeight(foot, 1f);
            animator.SetIKPosition(foot, animator.GetIKPosition(foot));
            animator.SetIKRotation(foot, animator.GetIKRotation(foot));
        }
    }
}
