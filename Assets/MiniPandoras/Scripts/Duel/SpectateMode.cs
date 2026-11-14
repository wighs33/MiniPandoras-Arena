using MiniPandoras.Cameras;
using MiniPandoras.Controls;
using UnityEngine;

namespace MiniPandoras.Duel
{
    /// <summary>
    /// 관전 모드(방송 화면): 두 인물을 모두 AI가 조작하고, 관전 카메라(<see cref="SpectatorCamera"/>)가 여러 각도로 담는다.
    /// 화면 조작과 방 메뉴는 숨기고 「관전 끝내기」만 남긴다. 판이 끝나면 잠시 뒤 저절로 다음 판을 세운다.
    /// 여는 버튼이 방 메뉴의 고를 것들 안에 있어서 방(온라인)에 있는 동안은 들어올 수 없다.
    /// </summary>
    public sealed class SpectateMode : MonoBehaviour
    {
        [SerializeField] private DuelMatch match;
        [SerializeField] private PlayerControls playerControls;
        [Tooltip("관전하는 동안 사람 대신 내 인물을 조작하는 AI")]
        [SerializeField] private RivalAi playerAi;
        [SerializeField] private RivalAi rivalAi;
        [SerializeField] private QuarterViewCamera battleCamera;
        [SerializeField] private SpectatorCamera spectatorCamera;
        [Tooltip("관전하는 동안 숨길 것(화면 조작, 방 메뉴)")]
        [SerializeField] private GameObject[] hiddenWhileWatching;
        [Tooltip("관전하는 동안만 보일 것(「관전 끝내기」)")]
        [SerializeField] private GameObject watchingOverlay;
        [Tooltip("승패가 난 뒤 다음 판을 세우기까지(초)")]
        [SerializeField] private float nextRoundDelay = 3f;

        // 다음 판을 세울 게임 시각. 기다리는 판이 없으면 음수. 승패 화면에서는 느린 화면이 끝나 게임 시간이 제 빠르기다.
        private float nextRoundAt = -1f;

        public void Enter()
        {
            Show(watching: true);
            match.UseControls(playerAi, rivalAi);
            match.RoundDecided += OnRoundDecided;
            match.BeginRound();
        }

        public void Exit()
        {
            Show(watching: false);
            match.UseControls(playerControls, rivalAi);
            match.RoundDecided -= OnRoundDecided;
            nextRoundAt = -1f;
            match.BeginRound();
        }

        private void Awake()
        {
            watchingOverlay.SetActive(false);
        }

        private void Show(bool watching)
        {
            battleCamera.enabled = !watching;
            spectatorCamera.enabled = watching;
            foreach (GameObject hidden in hiddenWhileWatching)
            {
                hidden.SetActive(!watching);
            }

            watchingOverlay.SetActive(watching);
        }

        private void OnRoundDecided(bool playerWon)
        {
            nextRoundAt = Time.time + nextRoundDelay;
        }

        private void Update()
        {
            if (nextRoundAt >= 0f && Time.time >= nextRoundAt)
            {
                nextRoundAt = -1f;
                match.Rematch();
            }
        }
    }
}
