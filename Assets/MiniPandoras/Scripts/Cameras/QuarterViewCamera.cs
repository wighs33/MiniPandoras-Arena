using MiniPandoras.Fighters;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiniPandoras.Cameras
{
    /// <summary>
    /// 두 인물을 비스듬히 내려다보며 함께 담는 쿼터뷰 대전 카메라. 둘 사이 가운데를 보고, 둘이 멀어지면 뒤로 물러난다.
    /// 각도는 돌리지 않는다(조이스틱의 위가 늘 화면의 위가 되게).
    /// 주인공이 맞거나 3연격 끝타를 맞히면 흔들린다(2D판과 같은 두 경우).
    /// 값은 화면 연출이라 게임 수치(Data/)가 아니라 장면의 이 컴포넌트에 둔다.
    /// </summary>
    public sealed class QuarterViewCamera : MonoBehaviour
    {
        [FormerlySerializedAs("target")]
        [SerializeField] private Fighter player;
        [Tooltip("함께 담을 상대")]
        [SerializeField] private Fighter rival;
        [Tooltip("내려다보는 각도(도)")]
        [SerializeField] private float pitch = 48f;
        [Tooltip("돌아본 각도(도). 0이면 월드 +Z 쪽을 본다")]
        [SerializeField] private float yaw = 45f;
        [Tooltip("두 사람이 가까울 때 보는 점에서 카메라까지 거리(m)")]
        [SerializeField] private float distance = 12f;
        [Tooltip("두 사람이 멀 때 카메라까지 거리(m)")]
        [SerializeField] private float farDistance = 15f;
        [Tooltip("두 사람 사이가 이 거리(m) 안이면 가장 가깝게 본다")]
        [SerializeField] private float nearGap = 2.5f;
        [Tooltip("두 사람 사이가 이 거리(m) 이상이면 가장 멀리 본다")]
        [SerializeField] private float farGap = 9f;
        [Tooltip("인물의 발에서 이만큼 위를 본다(m)")]
        [SerializeField] private float lookHeight = 0.7f;
        [Tooltip("따라붙는 데 걸리는 시간(초). 길수록 늦게 따라온다")]
        [SerializeField] private float followTime = 0.16f;
        [Tooltip("물러나고 다가서는 데 걸리는 시간(초)")]
        [SerializeField] private float zoomTime = 0.5f;

        [Header("흔들림")]
        [Tooltip("따라가는 인물이 맞았을 때 흔들리는 폭(m)과 시간(초). 2D판의 2px, 8프레임")]
        [SerializeField] private Vector2 hurtShake = new Vector2(0.12f, 0.13f);
        [Tooltip("끝타를 맞혔을 때 흔들리는 폭(m)과 시간(초). 2D판의 1.5px, 6프레임")]
        [SerializeField] private Vector2 finisherShake = new Vector2(0.09f, 0.1f);
        [Tooltip("흔들리는 빠르기(초당 왕복)")]
        [SerializeField] private float shakeFrequency = 30f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private float currentDistance;
        private float distanceVelocity;
        private float shakeStrength;
        private float shakeDuration;
        private float shakeLeft;

        /// <summary>바닥에서 본 화면 방향. 조이스틱 입력을 월드 방향으로 돌릴 때 쓴다.</summary>
        public Quaternion GroundRotation => Quaternion.Euler(0f, yaw, 0f);

        private void OnEnable()
        {
            player.Struck += OnPlayerStruck;
            player.Landed += OnPlayerLanded;
        }

        private void OnDisable()
        {
            player.Struck -= OnPlayerStruck;
            player.Landed -= OnPlayerLanded;
        }

        private void Start()
        {
            focus = FocusPoint();
            currentDistance = WantedDistance();
            Place();
        }

        private void LateUpdate()
        {
            focus = Vector3.SmoothDamp(focus, FocusPoint(), ref focusVelocity, followTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, WantedDistance(), ref distanceVelocity, zoomTime);
            shakeLeft = Mathf.Max(0f, shakeLeft - Time.deltaTime);
            Place();
        }

        private void OnPlayerStruck(Hit hit)
        {
            // 장판·화상처럼 나눠 들어오는 피해에는 흔들지 않는다(0.5초마다 흔들리면 어지럽다).
            if (hit.Staggers)
            {
                Shake(hurtShake);
            }
        }

        private void OnPlayerLanded(Hit hit)
        {
            if (hit.Finisher)
            {
                Shake(finisherShake);
            }
        }

        private void Shake(Vector2 strengthAndTime)
        {
            shakeStrength = strengthAndTime.x;
            shakeDuration = strengthAndTime.y;
            shakeLeft = shakeDuration;
        }

        /// <summary>두 사람 사이 가운데.</summary>
        private Vector3 FocusPoint()
        {
            Vector3 middle = (player.transform.position + rival.transform.position) * 0.5f;
            return middle + Vector3.up * lookHeight;
        }

        /// <summary>두 사람이 멀어질수록 뒤로 물러난다.</summary>
        private float WantedDistance()
        {
            Vector3 between = rival.transform.position - player.transform.position;
            between.y = 0f;
            float spread = Mathf.InverseLerp(nearGap, farGap, between.magnitude);
            return Mathf.Lerp(distance, farDistance, spread);
        }

        private void Place()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focus - rotation * Vector3.forward * currentDistance;
            transform.SetPositionAndRotation(position + rotation * ShakeOffset(), rotation);
        }

        /// <summary>화면 평면 위의 흔들림. 갈수록 줄어든다.</summary>
        private Vector3 ShakeOffset()
        {
            if (shakeLeft <= 0f)
            {
                return Vector3.zero;
            }

            float fade = shakeLeft / shakeDuration;
            float time = Time.time * shakeFrequency;
            float x = Mathf.PerlinNoise(time, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, time) * 2f - 1f;
            return new Vector3(x, y, 0f) * (shakeStrength * fade);
        }
    }
}
