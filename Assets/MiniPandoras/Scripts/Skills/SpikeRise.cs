using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 가시 하나가 땅에서 튀어 오르고, 잠깐 서 있다가 가라앉는다. 그림만 맡는다(맞았는지는 SkillTrap·SkillThorns가 정한다).
    /// </summary>
    public sealed class SpikeRise : MonoBehaviour
    {
        [Tooltip("튀어 오르는 시간(초)")]
        [SerializeField] private float riseTime = 0.08f;
        [Tooltip("다 솟은 채 서 있는 시간(초)")]
        [SerializeField] private float holdTime = 0.35f;
        [Tooltip("가라앉는 시간(초)")]
        [SerializeField] private float sinkTime = 0.25f;
        [Tooltip("솟을 때 넘치는 크기 배율")]
        [SerializeField] private float overshoot = 1.15f;

        private Vector3 fullScale;
        private float age;

        /// <summary>솟아서 가라앉아 사라질 때까지 걸리는 시간(초).</summary>
        public float Lifetime => riseTime + holdTime + sinkTime;

        /// <summary>그 키와 굵기로 솟게 한다(가시마다 조금씩 다르게).</summary>
        public void Raise(float height, float thickness)
        {
            fullScale = new Vector3(thickness, height, thickness);
            age = 0f;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float grow;
            if (age < riseTime)
            {
                grow = Mathf.Lerp(0f, overshoot, age / riseTime);
            }
            else if (age < riseTime + holdTime)
            {
                grow = Mathf.Lerp(overshoot, 1f, Mathf.Clamp01((age - riseTime) / 0.06f));
            }
            else
            {
                float sink = (age - riseTime - holdTime) / sinkTime;
                if (sink >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                grow = 1f - sink * sink;
            }

            // 굵기는 덜 줄여서 가라앉을 때 땅으로 빨려 드는 것처럼 보이게 한다.
            transform.localScale = new Vector3(fullScale.x * Mathf.Sqrt(grow), fullScale.y * grow, fullScale.z * Mathf.Sqrt(grow));
        }
    }
}
