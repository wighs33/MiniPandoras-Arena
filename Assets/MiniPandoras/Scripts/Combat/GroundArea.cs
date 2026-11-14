using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 공격이 닿는 자리를 바닥 평면(XZ)에서 잰다. 높이는 보지 않는다.
    /// 2D판이 칸 위 평면에서 재던 판정을 3D 바닥으로 옮긴 것이고, 그림(애니메이션)과 상관없이 같은 답을 낸다.
    /// </summary>
    public static class GroundArea
    {
        /// <summary>몸 반지름이 bodyRadius인 상대가 그 원에 닿는지.</summary>
        public static bool InCircle(Vector3 center, float radius, Vector3 body, float bodyRadius)
        {
            float x = body.x - center.x;
            float z = body.z - center.z;
            float reach = radius + bodyRadius;
            return x * x + z * z <= reach * reach;
        }

        /// <summary>from에서 to를 향하는 바닥 방향(길이 1). 두 점이 겹치면 fallback.</summary>
        public static Vector3 Direction(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
            return flat.sqrMagnitude > 0.0001f ? flat.normalized : fallback;
        }
    }
}
