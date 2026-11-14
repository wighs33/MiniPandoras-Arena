using MiniPandoras.Skills;
using MiniPandoras.UI;
using TMPro;
using UnityEngine;

namespace MiniPandoras.Duel
{
    /// <summary>
    /// 프로필 창: 닉네임 칸과 시작 판도라 셋(<see cref="PlayerProfile"/>). 여는 동안 판을 멈춘다(<see cref="WindowPause"/>).
    /// 닫을 때 판도라를 바꿨으면 그 판도라로 새 판을 세운다. 여는 버튼이 방 메뉴의 고를 것들 안에 있어서
    /// 방(온라인)에 있는 동안은 열 수 없다.
    /// </summary>
    public sealed class ProfileWindow : MonoBehaviour
    {
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private DuelMatch match;
        [SerializeField] private WindowPause pause;
        [Tooltip("어둠 판과 창. 열 때 켜고 닫을 때 끈다")]
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_InputField nicknameInput;
        [Tooltip("판도라 칸. 프로필의 판도라 순서대로")]
        [SerializeField] private PandoraCard[] cards;

        private PandoraDefinition openedWith;

        public void Open()
        {
            openedWith = profile.Pandora;
            nicknameInput.SetTextWithoutNotify(profile.Nickname);
            ShowSelection();
            window.SetActive(true);
            pause.Hold();
        }

        /// <summary>닫기 버튼과 창 바깥(어둠 판)을 누르면 닫는다.</summary>
        public void Close()
        {
            profile.Rename(nicknameInput.text);
            window.SetActive(false);
            pause.Release();
            if (profile.Pandora != openedWith)
            {
                match.BeginRound();
            }
        }

        private void Awake()
        {
            nicknameInput.characterLimit = PlayerProfile.MaxNicknameLength;
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Show(profile.Pandoras[i]);
            }

            window.SetActive(false);
        }

        private void OnEnable()
        {
            foreach (PandoraCard card in cards)
            {
                card.Picked += OnPicked;
            }
        }

        private void OnDisable()
        {
            foreach (PandoraCard card in cards)
            {
                card.Picked -= OnPicked;
            }
        }

        private void OnPicked(PandoraDefinition pandora)
        {
            profile.Choose(pandora);
            ShowSelection();
        }

        private void ShowSelection()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].ShowSelected(profile.Pandoras[i] == profile.Pandora);
            }
        }
    }
}
