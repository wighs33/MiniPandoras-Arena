using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// 같은 자리의 정점들(각진 면에서 갈라진 정점)의 법선을 평균해 접선 칸에 넣는다.
    /// 외곽선 패스가 이 방향으로 밀면 모서리에서 테두리가 끊기지 않는다. 접선은 스키닝을 따라 돌아서 움직이는 인물에도 맞다.
    /// </summary>
    public static class OutlineNormalBaker
    {
        // 블렌더가 내보낸 좌표의 소수점 오차를 같은 자리로 묶는 격자 크기(미터).
        private const float WeldGrid = 0.0001f;

        public static void Bake(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            var sums = new Dictionary<Vector3Int, Vector3>(positions.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3Int key = Weld(positions[i]);
                sums.TryGetValue(key, out Vector3 sum);
                sums[key] = sum + normals[i];
            }

            var tangents = new Vector4[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 smooth = sums[Weld(positions[i])].normalized;
                tangents[i] = new Vector4(smooth.x, smooth.y, smooth.z, 1f);
            }

            mesh.tangents = tangents;
        }

        private static Vector3Int Weld(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / WeldGrid),
                Mathf.RoundToInt(position.y / WeldGrid),
                Mathf.RoundToInt(position.z / WeldGrid));
        }
    }
}
