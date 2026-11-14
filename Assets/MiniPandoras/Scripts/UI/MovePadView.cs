using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 이동 패드가 손가락에 대답하게 한다. 손을 대지 않으면 맵을 덜 가리도록 옅게 두고, 손잡이에 손가락이 닿아 있는 동안은 또렷하게 한다
    /// (2D판 Docs/UI_UX_GUIDE.md 1장 「누르면 바로 대답한다」). 손잡이를 움직이는 것은 같은 오브젝트의 OnScreenStick이고, 여기서는 밝기만 바꾼다.
    /// 코드는 2D판(MiniPandoras-2D)에서 복사했고, 그림은 도트가 아니게 새로 그렸다(Tools/Textures/touch_controls.py).
    /// </summary>
    public sealed class MovePadView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // 2D판 hud_buttons.py의 미리보기(IDLE_ALPHA)와 같은 값이다.
        private const float IdleAlpha = 0.7f;
        // 또렷해지거나 옅어지는 데 걸리는 시간. 지침의 눌림 반응(0.1초 안)에 맞춘다.
        private const float FadeSeconds = 0.08f;

        [Tooltip("패드 전체(바탕과 손잡이)")]
        [SerializeField] private CanvasGroup pad;

        private bool held;

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
        }

        // 연출로 조작이 꺼졌다가 돌아오면 손을 뗀 옅은 패드로 시작한다.
        private void OnEnable()
        {
            held = false;
            pad.alpha = IdleAlpha;
        }

        private void Update()
        {
            float step = (1f - IdleAlpha) * Time.unscaledDeltaTime / FadeSeconds;
            pad.alpha = Mathf.MoveTowards(pad.alpha, held ? 1f : IdleAlpha, step);
        }
    }
}
