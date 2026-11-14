using MiniPandoras.Input;
using UnityEngine;

namespace MiniPandoras.Duel
{
    /// <summary>
    /// 창(프로필, 순위표)이 열린 동안 판을 멈추고 게임 입력을 쉬게 한다. 안 그러면 창 뒤에서 AI가 계속 때리고,
    /// 닉네임 칸에 친 Z·J·X가 공격으로, WASD가 이동으로도 들어간다. 창은 한 번에 하나만 열린다.
    /// </summary>
    public sealed class WindowPause : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader gameInput;

        // 쓰러짐의 느린 화면 도중에 열었으면 닫을 때 그 빠르기로 돌아간다.
        private float timeScaleBeforeHold;

        public void Hold()
        {
            gameInput.enabled = false;
            timeScaleBeforeHold = Time.timeScale;
            Time.timeScale = 0f;
        }

        public void Release()
        {
            gameInput.enabled = true;
            Time.timeScale = timeScaleBeforeHold;
        }
    }
}
