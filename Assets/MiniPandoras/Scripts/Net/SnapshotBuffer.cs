using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 한 인물의 모습을 호스트 시계의 시각과 함께 쌓아 두고, 그 사이 아무 시각의 모습을 메워 꺼낸다(보간).
    /// 참가자 화면은 호스트보다 조금 늦은 시각을 그리므로, 그 시각의 앞뒤로 받은 모습이 늘 둘 있다.
    /// 늦게 와서 순서가 뒤바뀐 모습은 버린다. 정해진 칸을 돌려 써서 받을 때마다 메모리를 만들지 않는다.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        // 한 틱 사이에 이보다 멀리 옮겨졌으면 순간이동(차원참, 숨었다 나옴, 새 판)이라 섞지 않는다.
        // 섞으면 그 사이를 미끄러져 지나가 보인다. 공간도약은 한 틱에 1m가 안 된다.
        private const float TeleportDistance = 2f;

        private readonly double[] times;
        private readonly FighterSnapshot[] snapshots;
        private int count;
        private int newest = -1;

        public SnapshotBuffer(int capacity)
        {
            times = new double[capacity];
            snapshots = new FighterSnapshot[capacity];
        }

        public int Count => count;

        public void Clear()
        {
            count = 0;
            newest = -1;
        }

        public void Add(double time, FighterSnapshot snapshot)
        {
            if (count > 0 && time <= times[newest])
            {
                return;
            }

            newest = (newest + 1) % times.Length;
            times[newest] = time;
            snapshots[newest] = snapshot;
            count = Mathf.Min(count + 1, times.Length);
        }

        /// <summary>
        /// 그 시각의 모습. 받은 것 가운데 가장 이른 것보다 이르면 가장 이른 것을, 가장 새것보다 늦으면 가장 새것을 준다
        /// (앞질러 짐작하지 않는다). 받은 것이 없으면 false.
        /// </summary>
        public bool TrySample(double time, out FighterSnapshot result)
        {
            for (int back = 0; back < count; back++)
            {
                int index = IndexFromNewest(back);
                if (times[index] > time)
                {
                    continue;
                }

                if (back == 0)
                {
                    result = snapshots[index];
                    return true;
                }

                int next = IndexFromNewest(back - 1);
                float share = (float)((time - times[index]) / (times[next] - times[index]));
                result = Blend(snapshots[index], snapshots[next], share);
                return true;
            }

            result = count > 0 ? snapshots[IndexFromNewest(count - 1)] : default;
            return count > 0;
        }

        private int IndexFromNewest(int back)
        {
            return (newest - back + times.Length) % times.Length;
        }

        private static FighterSnapshot Blend(FighterSnapshot from, FighterSnapshot to, float share)
        {
            // 섞을 수 없는 것(마나, 숨음)은 앞 모습 그대로 둔다.
            FighterSnapshot blended = from;
            if ((to.Position - from.Position).sqrMagnitude > TeleportDistance * TeleportDistance)
            {
                return blended;
            }

            blended.Position = Vector3.Lerp(from.Position, to.Position, share);
            blended.Yaw = Mathf.LerpAngle(from.Yaw, to.Yaw, share);
            blended.Speed = Mathf.Lerp(from.Speed, to.Speed, share);
            blended.GatePoint = Vector3.Lerp(from.GatePoint, to.GatePoint, share);
            return blended;
        }
    }
}
