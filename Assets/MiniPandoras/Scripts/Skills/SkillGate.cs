using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 차원이동: 쓴 사람이 틈에 숨어 있는 동안 바닥에 「나갈 곳」 표시를 띄운다. 사람은 조이스틱으로 표시를 옮기고
    /// 공격·스킬 버튼으로 나오며, AI는 자리를 정해 나온다. 고를 시간이 다 되면 표시가 있는 곳으로 나온다.
    /// 숨는 자리와 나오는 자리에서 틈이 열린다.
    /// </summary>
    public sealed class SkillGate : MonoBehaviour
    {
        // 나갈 곳 표시가 조이스틱을 끝까지 밀었을 때 움직이는 빠르기(m/s).
        private const float SteerSpeed = 7f;
        // 나온 뒤 맞지 않는 시간(초). 2D판의 30프레임.
        private const float SafeTime = 0.5f;

        [Tooltip("틈이 열리는 효과. 숨는 자리와 나오는 자리에 띄운다")]
        [SerializeField] private GameObject rift;

        private Fighter caster;
        private float left;
        private float reach;

        /// <summary>숨은 지 얼마나 됐는지(초). AI가 나올 때를 고를 때 쓴다.</summary>
        public float HiddenFor { get; private set; }

        /// <summary>아직 숨어 있다. 나오면 false가 되고, 그 프레임이 끝날 때 표시가 사라진다.</summary>
        public bool IsOpen { get; private set; }

        /// <param name="arenaReach">투기장 가운데에서 나갈 곳으로 고를 수 있는 거리(m)</param>
        public void Begin(Fighter owner, SkillDefinition skill, float arenaReach)
        {
            caster = owner;
            left = skill.Shape.Duration;
            reach = arenaReach;
            IsOpen = true;
            Instantiate(rift, owner.transform.position, Quaternion.identity);
            owner.Hide();
        }

        /// <summary>나갈 곳 표시를 그 바닥 방향(길이 0~1)으로 옮긴다.</summary>
        public void Steer(Vector3 direction, float deltaTime)
        {
            PlaceAt(transform.position + Vector3.ClampMagnitude(direction, 1f) * (SteerSpeed * deltaTime));
        }

        /// <summary>나갈 곳 표시를 그 자리에 둔다. 투기장 밖이면 안쪽 끝으로 당긴다.</summary>
        public void PlaceAt(Vector3 position)
        {
            var flat = new Vector3(position.x, 0f, position.z);
            flat = Vector3.ClampMagnitude(flat, reach);
            transform.position = new Vector3(flat.x, caster.transform.position.y, flat.z);
        }

        /// <summary>표시가 있는 곳으로 나온다. 같은 프레임에 두 번 불려도(공격·스킬 버튼을 함께 누름) 한 번만 나온다.</summary>
        public void Leave()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Instantiate(rift, transform.position, Quaternion.identity);
            caster.Reveal(transform.position, SafeTime);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            HiddenFor += Time.deltaTime;
            left -= Time.deltaTime;
            if (left <= 0f)
            {
                Leave();
            }
        }
    }
}
