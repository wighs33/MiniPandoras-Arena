using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 참가자의 내 인물은 조작에 바로 움직인다(예측). 이동 입력을 보낼 때마다 그때의 자리를 적어 두고,
    /// 호스트가 「그 입력까지 반영한 자리」를 알려 오면 적어 둔 자리와 견줘 어긋난 만큼 고친다(되맞춤).
    /// 고칠 몫은 한 번에 옮기지 않고 몇 프레임에 나눠 옮긴다. 순간이동만큼 크게 어긋났을 때만 한 번에 옮긴다.
    /// 고치기로 한 몫은 그 뒤에 적은 자리에도 미리 더해 둔다. 안 그러면 다음 답에서 같은 어긋남을 또 고친다.
    /// </summary>
    public sealed class PositionReconciler
    {
        // 이보다 작게 어긋나면 고치지 않는다. 호스트는 입력을 받은 뒤 다음 틱까지 그 입력으로 움직이므로,
        // 달리는 중(초속 4.4m)에는 틱 반쯤(약 0.07m)씩 늘 어긋나 보인다.
        private const float Tolerance = 0.12f;
        // 이보다 크게 어긋나면 나눠 옮기지 않고 바로 옮긴다(새 판, 순간이동).
        private const float SnapDistance = 2f;
        // 남은 몫의 이 비율(초당)만큼 옮긴다. 8이면 0.1초에 절반 남짓을 옮긴다.
        private const float CatchUpRate = 8f;

        private readonly int[] inputs;
        private readonly Vector3[] positions;
        private Vector3 pending;

        public PositionReconciler(int capacity)
        {
            inputs = new int[capacity];
            positions = new Vector3[capacity];
            Clear();
        }

        /// <summary>아직 옮기지 않은 고칠 몫.</summary>
        public Vector3 Pending => pending;

        public void Clear()
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = -1;
            }

            pending = Vector3.zero;
        }

        /// <summary>그 번호의 이동 입력을 보낼 때 내 인물이 있던 자리.</summary>
        public void Record(int input, Vector3 position)
        {
            int slot = input % inputs.Length;
            inputs[slot] = input;
            // 아직 옮기지 않은 몫까지 옮겼을 때의 자리로 적는다.
            positions[slot] = position + pending;
        }

        /// <summary>호스트가 그 번호의 입력까지 반영한 내 인물의 자리를 알려 왔다.</summary>
        public void Reconcile(int ackedInput, Vector3 hostPosition)
        {
            int slot = ackedInput % inputs.Length;
            if (ackedInput < 0 || inputs[slot] != ackedInput)
            {
                return;
            }

            Vector3 error = hostPosition - positions[slot];
            error.y = 0f;
            if (error.sqrMagnitude < Tolerance * Tolerance)
            {
                return;
            }

            pending += error;
            // 그 뒤에 적은 자리들도 같은 만큼 고쳐 둔다(이미 고치기로 했으니 다시 어긋남으로 세지 않는다).
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] > ackedInput)
                {
                    positions[i] += error;
                }
            }

            positions[slot] += error;
        }

        /// <summary>이번 프레임에 옮길 몫. 크게 어긋났으면 한 번에 다 준다.</summary>
        public Vector3 TakeCorrection(float deltaTime)
        {
            Vector3 step = pending.sqrMagnitude > SnapDistance * SnapDistance
                ? pending
                : pending * Mathf.Clamp01(CatchUpRate * deltaTime);
            pending -= step;
            return step;
        }
    }
}
