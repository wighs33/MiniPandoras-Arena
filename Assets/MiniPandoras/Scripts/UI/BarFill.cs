using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>체력·마나 막대의 채움 길이. 9-slice의 둥근 끝이 찌그러지지 않게 크기 대신 앵커로 늘인다.</summary>
    public static class BarFill
    {
        /// <param name="fromRight">오른쪽 끝에 붙은 막대(상대). 켜면 왼쪽부터 줄어든다.</param>
        public static void Stretch(RectTransform bar, float ratio, bool fromRight)
        {
            bar.anchorMin = fromRight ? new Vector2(1f - ratio, 0f) : Vector2.zero;
            bar.anchorMax = fromRight ? Vector2.one : new Vector2(ratio, 1f);
        }
    }
}
