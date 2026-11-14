using System;
using System.Threading.Tasks;
using MiniPandoras.Input;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 인터넷 너머의 상대와 방(세션)으로 잇는다. Multiplayer Services의 세션이 Unity Relay를 거쳐 Netcode의 호스트·참가자를
    /// 열어 주므로, 그다음은 주소로 이을 때와 같다(<see cref="DuelNetwork"/>). 로그인은 익명이다.
    /// 방 만들기는 방 코드를 보여 주고(코드로만 들어오는 방), 빠른 매칭은 열린 방에 들어가고 없으면 새로 열어 기다린다.
    /// 화면 위 방 메뉴(고를 것들, 「나가기」, 알림)를 보이고 숨기는 일도 여기서만 한다.
    /// </summary>
    public sealed class OnlineRoom : MonoBehaviour
    {
        private const int Seats = 2;
        // 빠른 매칭이 열린 방을 찾는 시간(초). 못 찾으면 방을 열고 기다린다.
        private const int QuickJoinSeconds = 5;

        [SerializeField] private NetworkManager network;
        [Tooltip("방에 들어가기 전에 고르는 것들(방 만들기, 빠른 매칭, 코드 칸, 참가)")]
        [SerializeField] private CanvasGroup choices;
        [Tooltip("방에 있는 동안 보이는 「나가기」")]
        [SerializeField] private GameObject leaveButton;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private TMP_Text status;
        [Tooltip("방 코드를 치는 동안 쉬게 할 게임 입력. 안 그러면 Z·J·X는 공격으로, WASD는 이동으로도 들어간다")]
        [SerializeField] private PlayerInputReader gameInput;

        private ISession session;

        /// <summary>
        /// 방에 상대가 있으면 그 사람의 플레이어 ID(Unity 인증)를 준다.
        /// 주소로 바로 이은 시험 연결에는 방이 없어서 false다(그런 판은 순위에 들지 않는다).
        /// </summary>
        public bool TryGetOpponent(out string playerId)
        {
            playerId = null;
            if (session == null)
            {
                return false;
            }

            foreach (IReadOnlyPlayer player in session.Players)
            {
                if (player.Id != session.CurrentPlayer.Id)
                {
                    playerId = player.Id;
                    return true;
                }
            }

            return false;
        }

        public async void CreateRoom()
        {
            if (!await SignIn("방을 만드는 중…"))
            {
                return;
            }

            try
            {
                var options = new SessionOptions { MaxPlayers = Seats, IsPrivate = true }.WithRelayNetwork();
                Enter(await MultiplayerService.Instance.CreateSessionAsync(options));
            }
            catch (Exception e)
            {
                Fail("방을 만들지 못했다", e);
            }
        }

        /// <summary>코드 칸에 넣은 방에 들어간다(「참가」 버튼).</summary>
        public void JoinByCode()
        {
            JoinRoom(codeInput.text);
        }

        public async void JoinRoom(string roomCode)
        {
            string code = roomCode.Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                ShowStatus("방 코드를 넣어 주세요");
                return;
            }

            if (!await SignIn("방에 들어가는 중…"))
            {
                return;
            }

            try
            {
                Enter(await MultiplayerService.Instance.JoinSessionByCodeAsync(code));
            }
            catch (Exception e)
            {
                Fail("그 코드의 방에 들어가지 못했다", e);
            }
        }

        public async void QuickMatch()
        {
            if (!await SignIn("상대를 찾는 중…"))
            {
                return;
            }

            try
            {
                var quickJoin = new QuickJoinOptions { CreateSession = true, Timeout = TimeSpan.FromSeconds(QuickJoinSeconds) };
                var options = new SessionOptions { MaxPlayers = Seats }.WithRelayNetwork();
                Enter(await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoin, options));
            }
            catch (Exception e)
            {
                Fail("빠른 매칭에 실패했다", e);
            }
        }

        /// <summary>방에서 나온다(주소로 이었을 때는 연결을 끊는다). 다시 AI 상대와 혼자 하게 된다.</summary>
        public async void Leave()
        {
            ISession leaving = session;
            Forget();
            if (leaving != null)
            {
                try
                {
                    await leaving.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"방에서 나오다 오류: {e.Message}");
                }
            }

            if (network.IsListening)
            {
                network.Shutdown();
            }
        }

        private void OnEnable()
        {
            network.OnClientStarted += OnStarted;
            network.OnClientStopped += OnStopped;
            codeInput.onSelect.AddListener(PauseGameInput);
            codeInput.onDeselect.AddListener(ResumeGameInput);
        }

        private void OnDisable()
        {
            network.OnClientStarted -= OnStarted;
            network.OnClientStopped -= OnStopped;
            codeInput.onSelect.RemoveListener(PauseGameInput);
            codeInput.onDeselect.RemoveListener(ResumeGameInput);
        }

        private void PauseGameInput(string text)
        {
            gameInput.enabled = false;
        }

        private void ResumeGameInput(string text)
        {
            gameInput.enabled = true;
        }

        // 처음 모습은 Awake에서 정한다. 명령줄(NetworkSession)이 Start에서 방을 열 때 그 모습을 덮지 않게.
        private void Awake()
        {
            ShowStatus("");
            ShowChoices();
        }

        /// <summary>Unity 서비스에 익명으로 로그인한다(처음 한 번). 그동안 고를 것들을 누르지 못하게 한다.</summary>
        private async Task<bool> SignIn(string busyMessage)
        {
            choices.interactable = false;
            ShowStatus(busyMessage);
            try
            {
                await ServicesSignIn.SignInAsync();
                return true;
            }
            catch (Exception e)
            {
                Fail("Unity 서비스에 로그인하지 못했다(프로젝트 연결을 확인)", e);
                return false;
            }
        }

        private void Enter(ISession joined)
        {
            session = joined;
            session.PlayerJoined += OnRoomChanged;
            session.PlayerHasLeft += OnRoomChanged;
            // 시험할 때 다른 창이 이 코드로 들어올 수 있게 로그에도 남긴다.
            Debug.Log($"[방] {(session.IsHost ? "열었다" : "들어왔다")}: 코드 {session.Code}, {(session.IsPrivate ? "비공개" : "공개")}, {session.PlayerCount}/{session.MaxPlayers}명");
            ShowRoom();
        }

        private void Forget()
        {
            if (session == null)
            {
                return;
            }

            session.PlayerJoined -= OnRoomChanged;
            session.PlayerHasLeft -= OnRoomChanged;
            session = null;
        }

        private void OnRoomChanged(string playerId)
        {
            ShowRoom();
        }

        private void OnStarted()
        {
            ShowRoom();
        }

        /// <summary>호스트가 사라졌거나 방을 나왔다: 다시 고를 수 있게 한다.</summary>
        private void OnStopped(bool wasHost)
        {
            Forget();
            ShowChoices();
        }

        private void ShowRoom()
        {
            choices.gameObject.SetActive(false);
            leaveButton.SetActive(true);
            // 상대를 기다리는 동안만 방 코드를 보인다. 둘이 모이면 알림을 지운다.
            bool waiting = session != null && session.IsHost && session.PlayerCount < Seats;
            ShowStatus(waiting ? WaitingMessage() : "");
        }

        private string WaitingMessage()
        {
            return session.IsPrivate ? $"방 코드 {session.Code} · 상대를 기다리는 중" : "열린 방에서 상대를 기다리는 중";
        }

        private void ShowChoices()
        {
            choices.gameObject.SetActive(true);
            choices.interactable = true;
            leaveButton.SetActive(false);
        }

        private void Fail(string message, Exception e)
        {
            Debug.LogWarning($"{message}: {e.Message}");
            ShowChoices();
            ShowStatus(message);
        }

        private void ShowStatus(string message)
        {
            status.SetText(message);
            status.gameObject.SetActive(message.Length > 0);
        }
    }
}
