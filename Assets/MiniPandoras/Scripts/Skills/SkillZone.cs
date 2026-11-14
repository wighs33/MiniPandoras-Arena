using MiniPandoras.Combat;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 바닥에 깔려 한동안 남는 장판(불꽃 고리, 불바다, 화염 낙하의 불 장판, 쓴 사람을 따라다니는 검은 날개).
    /// 정해진 간격마다 안에 선 상대에게 피해를 주고, 스킬에 있으면 불을 붙이거나 늦춘다.
    /// 고리 두께가 있으면 고리 위에 선 상대만 맞고, 퍼지는 시간이 있으면 몸에서 시작해 끝까지 퍼진다.
    /// 퍼지는 동안 가장자리가 지나가는 상대는 한 번 맞는다(정해진 간격 사이에 지나쳐 버리지 않게).
    /// 장판 피해는 휘청이지 않는다(빠져나올 수 있게).
    /// </summary>
    public sealed class SkillZone : MonoBehaviour
    {
        // 툰 효과 셰이더(ToonFx)에서 올릴수록 가장자리부터 타들어 가 사라지는 값.
        private static readonly int Erosion = Shader.PropertyToID("_Erosion");

        [Tooltip("바닥 그림. 한 변 1m인 사각형을 장판 크기에 맞춰 늘린다")]
        [SerializeField] private Renderer ground;
        [Tooltip("바닥 그림에서 가장자리(원이면 테두리, 고리면 고리 한가운데)까지의 반지름. 한 변에 대한 비율")]
        [SerializeField] private float groundEdge = 0.5f;
        [Tooltip("장판 둘레에 서는 벽(반지름 1m 원기둥). 없어도 된다. 높이는 그대로 두고 반지름만 맞춘다")]
        [SerializeField] private Renderer wall;
        [Tooltip("불꽃 입자. 모양(원이나 고리)의 반지름을 장판에 맞춘다")]
        [SerializeField] private ParticleSystem flames;
        [Tooltip("장판 1m²마다 1초에 피우는 불꽃 수")]
        [SerializeField] private float flamesPerSquareMeter = 5f;
        [Tooltip("마지막 피해 뒤 사라지는 시간(초)")]
        [SerializeField] private float fadeTime = 0.6f;

        private Fighter caster;
        private float burn;
        private float slow;
        private float tickPower;
        private bool follow;
        private float radius;
        private float band;
        private float growTime;
        private int ticksLeft;
        private float interval;
        private float wait;
        private float age;
        private bool sweptTarget;
        private float fadeLeft;
        private Material groundMaterial;
        private Material wallMaterial;

        /// <summary>장판을 깐다. 피해·화상·느려짐·횟수·간격은 스킬에서, 크기는 부르는 쪽이 정한다(도약은 장판 반지름이 따로다).</summary>
        /// <param name="ringBand">고리의 두께(m). 0이면 꽉 찬 원</param>
        /// <param name="followOwner">쓴 사람을 따라다니는지(검은 날개)</param>
        public void Begin(Fighter owner, SkillDefinition skill, float zoneRadius, float ringBand, float spreadTime, bool followOwner)
        {
            caster = owner;
            burn = skill.Burn;
            slow = skill.Slow;
            tickPower = skill.Shape.TickPower;
            ticksLeft = skill.Shape.Ticks;
            interval = skill.Shape.Interval;
            radius = zoneRadius;
            band = ringBand;
            growTime = spreadTime;
            follow = followOwner;
            fadeLeft = fadeTime;
            // 깔리자마자 한 번 태운다.
            wait = 0f;

            // 사라질 때 이 장판만 타들어 가게 재질을 복제한다(장판마다 한 번).
            groundMaterial = ground.material;
            wallMaterial = wall != null ? wall.material : null;
            Resize(CurrentRadius());
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            age += deltaTime;
            float currentRadius = CurrentRadius();
            if (follow)
            {
                transform.position = caster.transform.position;
            }

            if (ticksLeft > 0)
            {
                Resize(currentRadius);
                bool spreading = age < growTime;
                if (spreading && !sweptTarget && Covers(caster.Opponent, currentRadius))
                {
                    sweptTarget = true;
                    Scorch(currentRadius);
                }

                wait -= deltaTime;
                if (wait <= 0f)
                {
                    wait += interval;
                    ticksLeft--;
                    Scorch(currentRadius);
                    if (ticksLeft == 0)
                    {
                        flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }

                return;
            }

            // 마지막 피해 뒤에는 남은 불꽃이 잦아드는 동안 바닥과 벽이 가장자리부터 타들어 가 사라진다.
            fadeLeft -= deltaTime;
            float burntOut = 1f - Mathf.Clamp01(fadeLeft / fadeTime);
            groundMaterial.SetFloat(Erosion, burntOut);
            if (wallMaterial != null)
            {
                wallMaterial.SetFloat(Erosion, burntOut);
            }

            if (fadeLeft <= 0f)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>쓴 사람을 따라다니는 장판인지(검은 날개). 이런 장판은 도망쳐도 따라온다.</summary>
        public bool FollowsOwner => follow;

        /// <summary>아직 피해를 주는 중이고 그 대상이 장판 안에 서 있는지.</summary>
        public bool IsCovering(Fighter target)
        {
            return ticksLeft > 0 && Covers(target, CurrentRadius());
        }

        /// <summary>그 자리에서 장판을 가장 빨리 벗어나는 바닥 방향(고리 안쪽이면 가운데로, 아니면 바깥으로).</summary>
        public Vector3 WayOut(Vector3 position)
        {
            Vector3 outward = GroundArea.Direction(transform.position, position, Vector3.forward);
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            bool insideRing = band > 0f && offset.magnitude < CurrentRadius();
            return insideRing ? -outward : outward;
        }

        private void OnDestroy()
        {
            Destroy(groundMaterial);
            if (wallMaterial != null)
            {
                Destroy(wallMaterial);
            }
        }

        private float CurrentRadius()
        {
            return growTime > 0f ? radius * Mathf.Clamp01(age / growTime) : radius;
        }

        private void Scorch(float currentRadius)
        {
            Fighter target = caster.Opponent;
            if (target.IsDown || !Covers(target, currentRadius))
            {
                return;
            }

            target.TakeHit(caster.RollHit(target, tickPower, Vector3.zero, staggers: false));
            if (burn > 0f)
            {
                target.Burn(burn);
            }

            if (slow > 0f)
            {
                target.Slow(slow);
            }
        }

        private bool Covers(Fighter target, float currentRadius)
        {
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (band > 0f)
            {
                return Mathf.Abs(distance - currentRadius) <= band * 0.5f + target.BodyRadius;
            }

            return distance <= currentRadius + target.BodyRadius;
        }

        private void Resize(float currentRadius)
        {
            ground.transform.localScale = Vector3.one * (currentRadius / groundEdge);
            if (wall != null)
            {
                Transform wallTransform = wall.transform;
                wallTransform.localScale = new Vector3(currentRadius, wallTransform.localScale.y, currentRadius);
            }

            ParticleSystem.ShapeModule shape = flames.shape;
            ParticleSystem.EmissionModule emission = flames.emission;
            float area;
            if (band > 0f)
            {
                shape.radius = currentRadius;
                shape.donutRadius = band * 0.5f;
                area = 2f * Mathf.PI * currentRadius * band;
            }
            else
            {
                shape.radius = Mathf.Max(0.01f, currentRadius);
                area = Mathf.PI * currentRadius * currentRadius;
            }

            emission.rateOverTime = flamesPerSquareMeter * area;
        }
    }
}
