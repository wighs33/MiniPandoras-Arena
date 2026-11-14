using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 공간도약: 쓴 사람 몸에 붙어 내달리는 동안 지나치는 상대를 한 번 벤다. 몸이 미끄러지는 것은 Fighter가 하고(SkillMove),
    /// 이것은 베기와 꼬리 그림을 맡는다. 다 내달리면 꼬리를 떼어 남기고 사라진다.
    /// </summary>
    public sealed class SkillDash : MonoBehaviour
    {
        // 내달린 뒤 떼어 남긴 꼬리가 사라질 때까지(초).
        private const float TrailFade = 0.6f;

        private Fighter caster;
        private SkillDefinition skill;
        private float left;
        private bool sliced;

        public void Begin(Fighter owner, SkillDefinition definition)
        {
            caster = owner;
            skill = definition;
            left = definition.Shape.FlightTime;
            transform.SetParent(owner.transform, true);
        }

        private void Update()
        {
            if (left <= 0f)
            {
                return;
            }

            left -= Time.deltaTime;
            Fighter target = caster.Opponent;
            Vector3 body = caster.transform.position;
            if (!sliced && !target.IsDown && GroundArea.InCircle(body, skill.Shape.Size, target.transform.position, target.BodyRadius))
            {
                sliced = true;
                Vector3 away = GroundArea.Direction(body, target.transform.position, caster.transform.forward);
                target.TakeHit(caster.RollHit(target, skill.Power, away * skill.Shape.Knock, staggers: true));
                if (skill.Impact != null)
                {
                    Instantiate(skill.Impact, target.transform.position, Quaternion.identity);
                }
            }

            if (left <= 0f)
            {
                transform.SetParent(null, true);
                foreach (TrailRenderer streak in GetComponentsInChildren<TrailRenderer>())
                {
                    streak.emitting = false;
                }

                foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                Destroy(gameObject, TrailFade);
            }
        }
    }
}
