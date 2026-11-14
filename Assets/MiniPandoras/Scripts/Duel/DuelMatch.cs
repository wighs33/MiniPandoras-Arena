using MiniPandoras.Fighters;
using MiniPandoras.Input;
using MiniPandoras.Skills;
using MiniPandoras.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Duel
{
    /// <summary>
    /// 한 판의 흐름: 준비 → 시작 → 싸움 → 한쪽이 쓰러지면 잠깐 느려짐 → 승패 → 다시 하기.
    /// 인물과 조작(사람·AI)은 판을 모른다. 싸우는 동안만 조작을 켜고, 새 판에는 내 인물에게 프로필의 판도라를,
    /// 상대에게 무작위 판도라를 쥐여 주고 두 인물을 처음 자리에 다시 세운다.
    /// </summary>
    public sealed class DuelMatch : MonoBehaviour
    {
        private enum Phase
        {
            Ready,
            Fight,
            KnockOut,
            Result,
            // 온라인 상대와 끊겨 판이 멈췄다. 알림을 보인 뒤 혼자 하는 새 판을 세운다.
            Interrupted,
        }

        [SerializeField] private Fighter player;
        [SerializeField] private Fighter rival;
        [SerializeField] private Transform playerStart;
        [SerializeField] private Transform rivalStart;
        [Tooltip("싸우는 동안만 켜는 조작(사람의 PlayerControls, 상대의 RivalAi)")]
        [SerializeField] private Behaviour[] controls;
        [Tooltip("판이 끝난 뒤 공격 버튼으로도 다시 할 수 있게 듣는다")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private DuelBanner banner;
        [SerializeField] private Button rematchButton;

        [Header("판도라")]
        [Tooltip("내 인물은 새 판마다 프로필의 판도라를 든다")]
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private SkillCaster playerSkills;
        [SerializeField] private SkillCaster rivalSkills;
        [Tooltip("상대는 판마다 이 가운데 하나를 무작위로 든다")]
        [SerializeField] private PandoraDefinition[] rivalPandoras;

        [Header("시간 (초, 화면 연출)")]
        [Tooltip("「준비」를 보이는 시간")]
        [SerializeField] private float readyTime = 1.2f;
        [Tooltip("「시작!」을 보이는 시간")]
        [SerializeField] private float goTime = 0.7f;
        [Tooltip("쓰러진 순간 게임이 느려지는 배율")]
        [SerializeField] private float knockOutTimeScale = 0.3f;
        [Tooltip("느려진 채로 보여 주는 실제 시간")]
        [SerializeField] private float knockOutSlowTime = 0.9f;
        [Tooltip("승패를 띄운 뒤 공격 버튼을 다시 하기로 받기까지(마구 누르던 손에 바로 넘어가지 않게)")]
        [SerializeField] private float resultLockTime = 0.6f;
        [Tooltip("판이 끊긴 알림(「상대가 나갔다」)을 보이는 시간. 그 뒤 새 판을 세운다")]
        [SerializeField] private float interruptedTime = 2f;

        [Header("글자 색 (2D판 UI 지침)")]
        [SerializeField] private Color moonlightColor = new Color32(0xF3, 0xE9, 0xD8, 0xFF);
        [SerializeField] private Color winColor = new Color32(0xDE, 0xAF, 0x76, 0xFF);
        [SerializeField] private Color loseColor = new Color32(0xC8, 0x51, 0x3D, 0xFF);

        private Phase phase;
        private float phaseLeft;

        /// <summary>새 판을 세웠다(처음 판과 다시 하기 모두). 온라인에서 호스트가 참가자에게 알린다.</summary>
        public event System.Action RoundBegan;

        /// <summary>온라인 참가자가 다시 하기를 눌렀다. 판은 호스트만 세우므로 호스트에 부탁한다.</summary>
        public event System.Action RematchRequested;

        /// <summary>승패가 났다(true면 이 PC의 사람이 이겼다). 온라인 판이면 순위표에 보고한다.</summary>
        public event System.Action<bool> RoundDecided;

        /// <summary>새 판마다 상대에게 판도라를 무작위로 쥐여 주는지(AI 상대). 온라인에서는 사람마다 제 판도라를 든다.</summary>
        public bool RivalPicksRandomPandora { get; set; } = true;

        /// <summary>온라인 참가자인지. 그러면 다시 하기를 직접 하지 않고 호스트에 부탁한다(<see cref="RematchRequested"/>).</summary>
        public bool WaitsForHost { get; set; }

        /// <summary>다시 하기. 승패가 나온 뒤에만 듣는다.</summary>
        public void Rematch()
        {
            if (phase != Phase.Result || phaseLeft > 0f)
            {
                return;
            }

            if (WaitsForHost)
            {
                RematchRequested?.Invoke();
            }
            else
            {
                BeginRound();
            }
        }

        /// <summary>판을 그 자리에서 멈추고 까닭을 알린 뒤, 잠시 후 새 판을 세운다(온라인 상대와 끊겼을 때).</summary>
        public void Interrupt(string reason)
        {
            SetControls(false);
            Time.timeScale = 1f;
            rematchButton.gameObject.SetActive(false);
            banner.Show(reason, moonlightColor);
            Enter(Phase.Interrupted, interruptedTime);
        }

        /// <summary>
        /// 두 인물의 시작 자리를 맞바꾼다. 온라인 참가자 화면에서는 Player(이 PC의 사람)가 호스트 화면의 상대 자리에서 시작한다.
        /// </summary>
        public void SwapStartPoints()
        {
            (playerStart, rivalStart) = (rivalStart, playerStart);
        }

        /// <summary>싸우는 동안 켤 조작을 바꾼다(AI 상대 ↔ 네트워크 너머의 사람). 지금 켜 있던 조작은 끈다.</summary>
        public void UseControls(params Behaviour[] newControls)
        {
            bool fighting = phase == Phase.Fight;
            SetControls(false);
            controls = newControls;
            SetControls(fighting);
        }

        private void OnEnable()
        {
            player.KnockedOut += OnKnockedOut;
            rival.KnockedOut += OnKnockedOut;
            input.AttackPressed += Rematch;
            rematchButton.onClick.AddListener(Rematch);
        }

        private void OnDisable()
        {
            player.KnockedOut -= OnKnockedOut;
            rival.KnockedOut -= OnKnockedOut;
            input.AttackPressed -= Rematch;
            rematchButton.onClick.RemoveListener(Rematch);
            Time.timeScale = 1f;
        }

        private void Start()
        {
            BeginRound();
        }

        private void Update()
        {
            phaseLeft -= Time.deltaTime;
            if (phaseLeft > 0f)
            {
                return;
            }

            switch (phase)
            {
                case Phase.Ready:
                    StartFight();
                    break;
                case Phase.KnockOut:
                    ShowResult();
                    break;
                case Phase.Interrupted:
                    BeginRound();
                    break;
            }
        }

        /// <summary>새 판을 세운다: 두 인물을 처음 자리에 다시 세우고 「준비」부터 시작한다.</summary>
        public void BeginRound()
        {
            // 온라인 참가자 화면에서는 호스트가 알려 온 판도라를 이미 쥐여 두었다.
            if (!WaitsForHost)
            {
                playerSkills.Equip(profile.Pandora);
            }

            if (RivalPicksRandomPandora)
            {
                rivalSkills.Equip(rivalPandoras[Random.Range(0, rivalPandoras.Length)]);
            }

            player.Restart(playerStart.position, playerStart.rotation);
            rival.Restart(rivalStart.position, rivalStart.rotation);
            SetControls(false);
            rematchButton.gameObject.SetActive(false);
            banner.Show("준비", moonlightColor);
            Enter(Phase.Ready, readyTime);
            RoundBegan?.Invoke();
        }

        private void StartFight()
        {
            banner.Show("시작!", moonlightColor, goTime);
            SetControls(true);
            Enter(Phase.Fight, 0f);
        }

        private void OnKnockedOut()
        {
            if (phase != Phase.Fight)
            {
                return;
            }

            SetControls(false);
            Time.timeScale = knockOutTimeScale;
            // 판의 시간은 게임 시간으로 재므로(영상 촬영과 같은 시간을 쓰게), 느려진 만큼 줄여야 실제로 그 길이만큼 보인다.
            Enter(Phase.KnockOut, knockOutSlowTime * knockOutTimeScale);
        }

        private void ShowResult()
        {
            Time.timeScale = 1f;
            bool won = rival.IsDown;
            banner.Show(won ? "승리" : "패배", won ? winColor : loseColor);
            rematchButton.gameObject.SetActive(true);
            Enter(Phase.Result, resultLockTime);
            RoundDecided?.Invoke(won);
        }

        private void SetControls(bool enabledNow)
        {
            foreach (Behaviour control in controls)
            {
                control.enabled = enabledNow;
            }
        }

        private void Enter(Phase next, float seconds)
        {
            phase = next;
            phaseLeft = seconds;
        }
    }
}
