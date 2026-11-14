using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>머리카락이 뚫고 들어가지 못하는 공(머리, 목, 가슴, 어깨). 뼈 아래 자식으로 두어 몸을 따라 움직인다.</summary>
    public sealed class HairCollider : MonoBehaviour
    {
        [Tooltip("반지름(m)")]
        [SerializeField] private float radius = 0.1f;

        public Vector3 Center => transform.position;
        public float Radius => radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Center, radius);
        }
    }
}
