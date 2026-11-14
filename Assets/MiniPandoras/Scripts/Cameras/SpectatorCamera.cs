using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Cameras
{
    /// <summary>
    /// 관전(방송 화면) 카메라. 쿼터뷰처럼 한 각도에 머물지 않고, 중계 화면처럼 여러 컷을 번갈아 보인다:
    /// 넓게 돌기 → 옆에서 → 한 사람 어깨 너머 → … 컷은 몇 초마다 바꾸고, 치명타·끝타를 맞거나 쓰러지면
    /// 그 사람을 가까이 담는 컷으로 바로 바꾼다. 컷이 바뀔 때는 섞지 않고 바로 넘긴다(한 컷 안에서만 부드럽게 따라간다).
    /// 사람이 조작하지 않을 때만 쓴다(조이스틱의 위가 화면의 위가 아니게 되므로).
    /// </summary>
    public sealed class SpectatorCamera : MonoBehaviour
    {
        private enum Shot
        {
            // 높이서 두 사람을 내려다보며 천천히 돈다.
            Wide,
            // 두 사람을 잇는 선의 옆에서, 낮게 둘을 함께 담는다(격투 게임 화면).
            Side,
            // 한 사람의 등 뒤에서 상대를 본다.
            OverShoulder,
            // 크게 맞거나 쓰러진 사람을 가까이 낮게 담는다.
            Close,
        }

        // 저절로 바꾸는 컷의 차례. 가까운 컷은 일이 있을 때만 쓴다.
        private static readonly Shot[] Rotation = { Shot.Wide, Shot.Side, Shot.OverShoulder, Shot.Side, Shot.Wide, Shot.OverShoulder };

        [SerializeField] private Camera view;
        [SerializeField] private Fighter first;
        [SerializeField] private Fighter second;

        [Header("컷 길이 (초, 실제 시간)")]
        [SerializeField] private float shotTime = 4.5f;
        [Tooltip("크게 맞은 사람을 가까이 담는 시간")]
        [SerializeField] private float closeTime = 1.4f;
        [Tooltip("쓰러진 사람을 가까이 담는 시간(느려진 화면이 끝날 때까지)")]
        [SerializeField] private float knockOutTime = 2.4f;
        [Tooltip("컷을 바꾼 뒤 이만큼은 다음 일로 다시 바꾸지 않는다. 연타마다 컷이 바뀌면 어지럽다")]
        [SerializeField] private float minShotTime = 1.2f;

        [Header("컷마다 거리·높이 (m)·시야각")]
        [SerializeField] private float wideDistance = 13f;
        [SerializeField] private float wideHeight = 7.5f;
        [Tooltip("넓은 컷이 도는 빠르기(초당 도)")]
        [SerializeField] private float wideOrbitSpeed = 9f;
        [SerializeField] private float sideDistance = 7.5f;
        [SerializeField] private float sideHeight = 1.6f;
        [SerializeField] private float shoulderBack = 3.6f;
        [SerializeField] private float shoulderSide = 1.3f;
        [SerializeField] private float shoulderHeight = 2.1f;
        [SerializeField] private float closeDistance = 3.4f;
        [SerializeField] private float closeHeight = 1.3f;
        [Tooltip("쓰러지는 사람은 이 거리·높이에서 허리 아래를 보며 20°쯤 내려다본다. 넘어가는 몸과 누운 몸이 함께 화면에 든다. " +
                 "서 있는 높이로 보면 누운 몸이 화면 아래로 빠지고, 바로 위에서 보면 넘어가는 동안 머리 꼭대기만 보인다")]
        [SerializeField] private float downDistance = 3.6f;
        [SerializeField] private float downHeight = 1.8f;
        [SerializeField] private float wideFieldOfView = 34f;
        [SerializeField] private float sideFieldOfView = 32f;
        [SerializeField] private float shoulderFieldOfView = 36f;
        [SerializeField] private float closeFieldOfView = 34f;

        [Header("따라가기")]
        [Tooltip("한 컷 안에서 따라붙는 데 걸리는 시간(초)")]
        [SerializeField] private float followTime = 0.3f;
        [Tooltip("인물의 발에서 이만큼 위를 본다(m)")]
        [SerializeField] private float lookHeight = 1f;
        [Tooltip("투기장 가운데에서 카메라가 나가지 않을 거리(m). 벽 밖으로 나가면 벽에 가린다")]
        [SerializeField] private float arenaRadius = 8.2f;

        private Shot shot;
        // 어깨 너머·가까운 컷이 담는 사람.
        private Fighter subject;
        private int rotationIndex;
        private float shotLeft;
        private float shotAge;
        private float orbitAngle;
        // 옆 컷이 두 사람을 잇는 선의 어느 쪽에 서는지(+1, -1).
        private float sideSign = 1f;
        private Vector3 position;
        private Vector3 positionVelocity;
        private Vector3 lookAt;
        private Vector3 lookVelocity;
        private float fieldOfViewBefore;

        private void OnEnable()
        {
            fieldOfViewBefore = view.fieldOfView;
            first.Struck += OnFirstStruck;
            second.Struck += OnSecondStruck;
            first.KnockedOut += OnFirstKnockedOut;
            second.KnockedOut += OnSecondKnockedOut;
            orbitAngle = Random.Range(0f, 360f);
            Cut(Shot.Wide, first, shotTime);
        }

        private void OnDisable()
        {
            first.Struck -= OnFirstStruck;
            second.Struck -= OnSecondStruck;
            first.KnockedOut -= OnFirstKnockedOut;
            second.KnockedOut -= OnSecondKnockedOut;
            view.fieldOfView = fieldOfViewBefore;
        }

        private void OnFirstStruck(Hit hit) => OnStruck(first, hit);

        private void OnSecondStruck(Hit hit) => OnStruck(second, hit);

        private void OnFirstKnockedOut() => Cut(Shot.Close, first, knockOutTime);

        private void OnSecondKnockedOut() => Cut(Shot.Close, second, knockOutTime);

        private void OnStruck(Fighter struck, Hit hit)
        {
            bool bigHit = hit.Critical || hit.Finisher;
            if (bigHit && shotAge >= minShotTime && !struck.IsDown && !struck.IsHidden)
            {
                Cut(Shot.Close, struck, closeTime);
            }
        }

        private void LateUpdate()
        {
            // 쓰러짐의 느린 화면에서도 컷 길이가 늘어나지 않게, 느려진 배율을 되돌린 시간으로 잰다.
            // unscaledDeltaTime은 영상 촬영(captureFramerate) 때 촬영 시간이 아니라 실제 시간이라 쓰지 않는다.
            float realDelta = Time.timeScale > 0f ? Time.deltaTime / Time.timeScale : 0f;
            shotLeft -= realDelta;
            shotAge += realDelta;
            orbitAngle += wideOrbitSpeed * realDelta;
            // 담던 사람이 차원이동으로 숨으면 빈자리를 담게 되므로 바로 다음 컷으로 넘긴다.
            bool subjectGone = (shot == Shot.Close || shot == Shot.OverShoulder) && subject.IsHidden;
            if (shotLeft <= 0f || subjectGone)
            {
                NextShot();
            }

            Aim(out Vector3 wantedPosition, out Vector3 wantedLook);
            position = Vector3.SmoothDamp(position, wantedPosition, ref positionVelocity, followTime, Mathf.Infinity, realDelta);
            lookAt = Vector3.SmoothDamp(lookAt, wantedLook, ref lookVelocity, followTime, Mathf.Infinity, realDelta);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }

        private void NextShot()
        {
            rotationIndex = (rotationIndex + 1) % Rotation.Length;
            Shot next = Rotation[rotationIndex];
            // 어깨 너머 컷은 두 사람을 번갈아 담는다.
            Fighter nextSubject = subject == first ? second : first;
            Cut(next, nextSubject, shotTime);
        }

        /// <summary>컷을 바꾼다. 새 컷의 자리로 바로 옮긴다(섞지 않는다).</summary>
        private void Cut(Shot next, Fighter nextSubject, float duration)
        {
            shot = next;
            subject = nextSubject;
            shotLeft = duration;
            shotAge = 0f;
            sideSign = Random.value < 0.5f ? -1f : 1f;
            view.fieldOfView = FieldOfView(next);

            Aim(out position, out lookAt);
            positionVelocity = Vector3.zero;
            lookVelocity = Vector3.zero;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }

        private float FieldOfView(Shot of)
        {
            switch (of)
            {
                case Shot.Side: return sideFieldOfView;
                case Shot.OverShoulder: return shoulderFieldOfView;
                case Shot.Close: return closeFieldOfView;
                default: return wideFieldOfView;
            }
        }

        /// <summary>지금 컷이 원하는 카메라 자리와 보는 점.</summary>
        private void Aim(out Vector3 wantedPosition, out Vector3 wantedLook)
        {
            Vector3 a = first.transform.position;
            Vector3 b = second.transform.position;
            Vector3 middle = (a + b) * 0.5f;
            Vector3 between = b - a;
            between.y = 0f;
            float gap = between.magnitude;
            Vector3 across = gap > 0.01f ? between / gap : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, across);

            switch (shot)
            {
                case Shot.Side:
                    // 두 사람이 멀어지면 그만큼 물러나 둘 다 화면에 남게 한다.
                    wantedPosition = middle + side * (sideSign * (sideDistance + gap * 0.6f)) + Vector3.up * sideHeight;
                    wantedLook = middle + Vector3.up * lookHeight;
                    break;
                case Shot.OverShoulder:
                {
                    Fighter other = subject == first ? second : first;
                    Vector3 toOther = other.transform.position - subject.transform.position;
                    toOther.y = 0f;
                    Vector3 forward = toOther.sqrMagnitude > 0.0001f ? toOther.normalized : subject.transform.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    wantedPosition = subject.transform.position - forward * shoulderBack + right * shoulderSide + Vector3.up * shoulderHeight;
                    wantedLook = other.transform.position + Vector3.up * lookHeight;
                    break;
                }
                case Shot.Close when subject.IsDown:
                    wantedPosition = subject.transform.position + side * (sideSign * downDistance) + Vector3.up * downHeight;
                    wantedLook = subject.transform.position + Vector3.up * 0.5f;
                    break;
                case Shot.Close:
                {
                    // 얼굴 쪽에서 70° 비켜 선다. 얼굴 바로 앞에는 때린 사람이 서 있어서, 덜 비키면 그 사람이 앞을 가린다.
                    Vector3 facing = subject == first ? across : -across;
                    Vector3 offset = Quaternion.Euler(0f, 70f * sideSign, 0f) * facing;
                    wantedPosition = subject.transform.position + offset * closeDistance + Vector3.up * closeHeight;
                    wantedLook = subject.transform.position + Vector3.up * lookHeight;
                    break;
                }
                default:
                {
                    Vector3 orbit = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.back;
                    wantedPosition = middle + orbit * wideDistance + Vector3.up * wideHeight;
                    wantedLook = middle + Vector3.up * lookHeight;
                    break;
                }
            }

            wantedPosition = InsideArena(wantedPosition, wantedLook);
        }

        /// <summary>카메라가 투기장 벽 밖으로 나가면 보는 점 쪽으로 당겨 들인다(벽에 가리지 않게).</summary>
        private Vector3 InsideArena(Vector3 wanted, Vector3 target)
        {
            Vector3 flat = new Vector3(wanted.x, 0f, wanted.z);
            if (flat.magnitude <= arenaRadius)
            {
                return wanted;
            }

            // 보는 점에서 카메라 쪽으로 가는 선이 벽 안쪽 원과 만나는 곳까지만 간다.
            Vector3 from = new Vector3(target.x, 0f, target.z);
            Vector3 toward = flat - from;
            float t = 1f;
            for (int i = 0; i < 20 && (from + toward * t).magnitude > arenaRadius; i++)
            {
                t -= 0.05f;
            }

            return target + (wanted - target) * Mathf.Max(t, 0.2f);
        }
    }
}
