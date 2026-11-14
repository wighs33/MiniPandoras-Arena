using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 곧게 날아가는 탄(불씨탄)이나 뻗는 손(검은 손). 바닥과 나란히 날아가다 상대에게 닿으면 터지고, 정해진 거리를 가면 사라진다.
    /// 손(Grab)은 맞힌 상대를 쓴 사람 몸 앞으로 끌어온다. 뛰어올라 맞지 않는 상대는 지나간다.
    /// </summary>
    public sealed class SkillProjectile : MonoBehaviour
    {
        // 끌려온 상대가 서는 자리: 쓴 사람 몸 앞 이만큼(m, 몸 가운데끼리).
        private const float PullStop = 0.9f;

        // 탄이 사라진 뒤 떼어 남긴 꼬리(입자, 선)가 사라질 때까지(초).
        private const float LeftoverTime = 0.8f;

        [Tooltip("쓴 사람과 손을 잇는 팔(검은 손). 없어도 된다")]
        [SerializeField] private LineRenderer tether;

        private Fighter caster;
        private SkillDefinition skill;
        private Vector3 velocity;
        private float travelLeft;

        public void Launch(Fighter owner, SkillDefinition definition, Vector3 direction)
        {
            caster = owner;
            skill = definition;
            velocity = direction * definition.Shape.Speed;
            travelLeft = definition.Shape.Range;
            transform.rotation = Quaternion.LookRotation(direction);
            DrawTether();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            transform.position += velocity * deltaTime;
            travelLeft -= velocity.magnitude * deltaTime;
            DrawTether();

            Fighter target = caster.Opponent;
            bool touches = GroundArea.InCircle(transform.position, skill.Shape.Radius, target.transform.position, target.BodyRadius);
            if (touches && !target.IsDown && !target.IsInvulnerable)
            {
                HitTarget(target);
                Finish();
            }
            else if (travelLeft <= 0f)
            {
                Finish();
            }
        }

        private void HitTarget(Fighter target)
        {
            target.TakeHit(caster.RollHit(target, skill.Power, PushFor(target), staggers: true));
            if (skill.Burn > 0f)
            {
                target.Burn(skill.Burn);
            }

            if (skill.Slow > 0f)
            {
                target.Slow(skill.Slow);
            }

            if (skill.Impact != null)
            {
                Instantiate(skill.Impact, transform.position, Quaternion.identity);
            }
        }

        /// <summary>탄은 날아가던 쪽으로 밀어내고, 손은 쓴 사람 몸 앞까지 끌어온다.</summary>
        private Vector3 PushFor(Fighter target)
        {
            if (skill.Shape.Kind != SkillKind.Grab)
            {
                return velocity.normalized * skill.Shape.Knock;
            }

            Vector3 casterFeet = caster.transform.position;
            Vector3 toTarget = GroundArea.Direction(casterFeet, target.transform.position, caster.transform.forward);
            Vector3 stop = casterFeet + toTarget * PullStop;
            Vector3 pull = stop - target.transform.position;
            pull.y = 0f;
            return pull;
        }

        private void DrawTether()
        {
            if (tether == null)
            {
                return;
            }

            // 팔은 쓴 사람의 뻗은 손(CastPoint)에서 시작한다.
            tether.SetPosition(0, caster.CastPoint);
            tether.SetPosition(1, transform.position);
        }

        /// <summary>탄은 바로 사라지고, 꼬리 입자와 꼬리 선은 떼어 놓아 남은 만큼 잦아들게 한다(뚝 끊기지 않게).</summary>
        private void Finish()
        {
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
            {
                particles.transform.SetParent(null, true);
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(particles.gameObject, LeftoverTime);
            }

            foreach (TrailRenderer streak in GetComponentsInChildren<TrailRenderer>())
            {
                streak.transform.SetParent(null, true);
                streak.emitting = false;
                Destroy(streak.gameObject, LeftoverTime);
            }

            Destroy(gameObject);
        }
    }
}
