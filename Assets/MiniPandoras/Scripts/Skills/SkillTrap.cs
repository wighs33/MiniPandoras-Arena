using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 상대 발밑에 깔려 잠깐 뒤 가시가 솟는 덫(가시 덫). 깔린 동안 바닥 고리가 점점 밝아지며 터질 때를 알리고,
    /// 터질 때 고리 안에 선 상대를 맞혀 늦춘다. 알려 주는 시간이 있어서 보고 피할 수 있다.
    /// </summary>
    public sealed class SkillTrap : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("예고 고리. 한 변 1m인 사각형을 덫 크기에 맞춰 늘린다")]
        [SerializeField] private Renderer warning;
        [Tooltip("예고 고리 그림에서 고리 한가운데까지의 반지름. 한 변에 대한 비율")]
        [SerializeField] private float warningEdge = 0.4f;
        [SerializeField] private SpikeRise spike;
        [Tooltip("터질 때 솟는 가시 수")]
        [SerializeField] private int spikeCount = 9;

        private Fighter caster;
        private SkillDefinition skill;
        private float left;
        private Material warningMaterial;
        private Color warningColor;

        public void Begin(Fighter owner, SkillDefinition definition)
        {
            caster = owner;
            skill = definition;
            left = definition.Shape.Delay;
            warning.transform.localScale = Vector3.one * (definition.Shape.Radius / warningEdge);
            // 덫마다 따로 밝아지게 재질을 복제한다(덫마다 한 번).
            warningMaterial = warning.material;
            warningColor = warningMaterial.GetColor(BaseColor);
            Brighten(0f);
        }

        private void Update()
        {
            left -= Time.deltaTime;
            Brighten(1f - Mathf.Clamp01(left / skill.Shape.Delay));
            if (left <= 0f)
            {
                Erupt();
            }
        }

        private void OnDestroy()
        {
            Destroy(warningMaterial);
        }

        /// <summary>터질 때가 다가올수록 고리가 밝아진다.</summary>
        private void Brighten(float progress)
        {
            warningMaterial.SetColor(BaseColor, warningColor * Mathf.Lerp(0.25f, 1f, progress * progress));
        }

        private void Erupt()
        {
            SkillShape shape = skill.Shape;
            Fighter target = caster.Opponent;
            if (!target.IsDown && GroundArea.InCircle(transform.position, shape.Radius, target.transform.position, target.BodyRadius))
            {
                Vector3 away = GroundArea.Direction(transform.position, target.transform.position, Vector3.forward);
                target.TakeHit(caster.RollHit(target, skill.Power, away * shape.Knock, staggers: true));
                if (skill.Slow > 0f)
                {
                    target.Slow(skill.Slow);
                }
            }

            RaiseSpikes(shape.Radius);
            if (skill.Impact != null)
            {
                Instantiate(skill.Impact, transform.position, Quaternion.identity);
            }

            // 가시는 이 덫의 자식이라, 덫은 가시가 가라앉을 때까지 남는다(예고 고리만 끈다).
            // 새 판이 서면 쓴 사람(SkillCaster)이 덫을 지우는데, 그때 아직 서 있는 가시도 함께 지워지게 하려는 것이다.
            warning.enabled = false;
            enabled = false;
            Destroy(gameObject, spike.Lifetime);
        }

        /// <summary>가운데에 큰 가시 하나, 둘레에 작은 가시들. 가시는 스스로 가라앉아 사라진다.</summary>
        private void RaiseSpikes(float radius)
        {
            for (int i = 0; i < spikeCount; i++)
            {
                Vector2 spot = i == 0 ? Vector2.zero : Random.insideUnitCircle * radius * 0.8f;
                Vector3 position = transform.position + new Vector3(spot.x, 0f, spot.y);
                Quaternion lean = Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(0f, 360f), Random.Range(-14f, 14f));
                SpikeRise instance = Instantiate(spike, position, lean, transform);
                float height = i == 0 ? 1.8f : Random.Range(0.9f, 1.5f);
                instance.Raise(height, i == 0 ? 1.7f : Random.Range(1.1f, 1.5f));
            }
        }
    }
}
