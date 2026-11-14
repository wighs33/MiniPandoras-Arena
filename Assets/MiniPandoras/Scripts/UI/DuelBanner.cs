using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 가운데에 큰 글자로 판의 순간을 알린다(준비, 시작!, 승리, 패배). 글자는 크게 튀어나왔다가 제 크기로 줄고,
    /// 보일 시간을 주면 그 뒤에 사라진다.
    /// </summary>
    public sealed class DuelBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [Tooltip("처음 튀어나올 때의 크기 배율")]
        [SerializeField] private float popScale = 1.5f;
        [Tooltip("튀어나온 크기에서 제 크기로 줄어드는 시간(초)")]
        [SerializeField] private float popTime = 0.18f;
        [Tooltip("사라지는 데 걸리는 시간(초)")]
        [SerializeField] private float fadeTime = 0.25f;

        private float age;
        // 0이면 Hide를 부를 때까지 남는다.
        private float lifetime;

        /// <param name="seconds">보일 시간(초). 0이면 Hide를 부를 때까지 남는다.</param>
        public void Show(string text, Color color, float seconds = 0f)
        {
            label.SetText(text);
            label.color = color;
            age = 0f;
            lifetime = seconds;
            group.alpha = 1f;
            gameObject.SetActive(true);
            Animate();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            age += Time.deltaTime;
            Animate();
            if (lifetime > 0f && age >= lifetime)
            {
                Hide();
            }
        }

        private void Animate()
        {
            float pop = Mathf.Lerp(popScale, 1f, Mathf.Clamp01(age / popTime));
            label.rectTransform.localScale = Vector3.one * pop;
            if (lifetime > 0f)
            {
                group.alpha = Mathf.Clamp01((lifetime - age) / fadeTime);
            }
        }
    }
}
