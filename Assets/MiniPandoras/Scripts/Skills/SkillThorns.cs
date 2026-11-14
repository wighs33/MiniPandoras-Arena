using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 쓴 자리 둘레에서 바깥으로 가시가 겹겹이 번진다(가시 숲). 겹마다 그 고리 위에 선 상대를 한 번 맞혀 바깥으로 민다.
    /// 가시는 이 오브젝트의 자식이고, 이 오브젝트는 마지막 가시가 가라앉을 때까지 남는다.
    /// 새 판이 서면 쓴 사람(<see cref="SkillCaster"/>)이 이 오브젝트를 지우는데, 그때 아직 서 있는 가시도 함께 지워지게 하려는 것이다.
    /// </summary>
    public sealed class SkillThorns : MonoBehaviour
    {
        // 가시가 바깥으로 기우는 각도(도). 번져 나가는 쪽이 읽히게 한다.
        private const float OutwardLean = 14f;

        [SerializeField] private SpikeRise spike;
        [Tooltip("한 겹에서 가시 사이 간격(m)")]
        [SerializeField] private float spacing = 0.85f;

        private Fighter caster;
        private SkillDefinition skill;
        private int ring;
        private float wait;

        public void Begin(Fighter owner, SkillDefinition definition)
        {
            caster = owner;
            skill = definition;
        }

        private void Update()
        {
            wait -= Time.deltaTime;
            if (wait > 0f)
            {
                return;
            }

            RaiseRing(ring);
            ring++;
            wait += skill.Shape.Interval;
            if (ring >= skill.Shape.Ticks)
            {
                // 더 솟지 않게 멈추고, 마지막 겹의 가시가 가라앉은 뒤에 지운다.
                enabled = false;
                Destroy(gameObject, spike.Lifetime);
            }
        }

        private void RaiseRing(int index)
        {
            SkillShape shape = skill.Shape;
            float ringRadius = shape.Radius * (index + 1) / shape.Ticks;
            int count = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * ringRadius / spacing));
            // 겹마다 가시 자리를 엇갈리게 한다.
            float offset = Random.value * Mathf.PI * 2f / count;
            for (int k = 0; k < count; k++)
            {
                float angle = offset + Mathf.PI * 2f * k / count;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Quaternion lean = Quaternion.AngleAxis(OutwardLean, Vector3.Cross(Vector3.up, outward)) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                SpikeRise instance = Instantiate(spike, transform.position + outward * ringRadius, lean, transform);
                instance.Raise(Random.Range(0.8f, 1.2f), Random.Range(1f, 1.3f));
            }

            Fighter target = caster.Opponent;
            Vector3 offsetToTarget = target.transform.position - transform.position;
            offsetToTarget.y = 0f;
            bool onRing = Mathf.Abs(offsetToTarget.magnitude - ringRadius) <= shape.Size + target.BodyRadius;
            if (onRing && !target.IsDown)
            {
                Vector3 away = GroundArea.Direction(transform.position, target.transform.position, Vector3.forward);
                target.TakeHit(caster.RollHit(target, skill.Power, away * shape.Knock, staggers: true));
            }
        }
    }
}
