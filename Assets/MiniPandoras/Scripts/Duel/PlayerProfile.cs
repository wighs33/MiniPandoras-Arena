using System.Collections.Generic;
using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.Duel
{
    /// <summary>
    /// 이 PC 사람의 프로필: 닉네임과 처음 드는 판도라. 이 PC에 저장해 두어 다음에 켤 때도 남는다(PlayerPrefs).
    /// 새 판마다 이 판도라를 들고(<see cref="DuelMatch"/>), 온라인에서는 닉네임과 판도라를 상대에게 알린다.
    /// </summary>
    public sealed class PlayerProfile : MonoBehaviour
    {
        public const int MaxNicknameLength = 10;
        private const string NicknameKey = "profile.nickname";
        private const string PandoraKey = "profile.pandora";

        [Tooltip("고를 수 있는 판도라(프로필 창에 이 순서로 보인다)")]
        [SerializeField] private PandoraDefinition[] pandoras;
        [Tooltip("처음 켰을 때 드는 판도라")]
        [SerializeField] private PandoraDefinition firstPandora;
        [Tooltip("처음 켰을 때의 닉네임 앞부분. 뒤에 숫자 넷을 붙여 사람마다 다르게 한다")]
        [SerializeField] private string firstNickname = "도전자";

        public string Nickname { get; private set; }
        public PandoraDefinition Pandora { get; private set; }
        public IReadOnlyList<PandoraDefinition> Pandoras => pandoras;

        /// <summary>
        /// 닉네임 규칙: 앞뒤 빈칸을 지우고 <see cref="MaxNicknameLength"/>자까지 자른다. 남은 것이 없으면 null이다.
        /// 온라인에서 상대가 보낸 이름도 이 규칙으로 거른다.
        /// </summary>
        public static string CleanNickname(string text)
        {
            string trimmed = text?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            return trimmed.Length > MaxNicknameLength ? trimmed.Substring(0, MaxNicknameLength).TrimEnd() : trimmed;
        }

        /// <summary>닉네임을 바꾼다. 규칙에 맞는 글이 남지 않으면 그대로 둔다.</summary>
        public void Rename(string text)
        {
            string nickname = CleanNickname(text);
            if (nickname == null)
            {
                return;
            }

            Nickname = nickname;
            PlayerPrefs.SetString(NicknameKey, Nickname);
        }

        public void Choose(PandoraDefinition pandora)
        {
            Pandora = pandora;
            PlayerPrefs.SetString(PandoraKey, pandora.name);
        }

        /// <summary>
        /// 에셋 이름(Flame, Demon, Dimension)으로 판도라를 찾는다. 저장해 둔 것과 순위표에 적힌 것은
        /// 화면 이름이 아니라 에셋 이름이다. 없으면 null이다.
        /// </summary>
        public PandoraDefinition PandoraNamed(string assetName)
        {
            return System.Array.Find(pandoras, pandora => pandora.name == assetName);
        }

        // 다른 컴포넌트가 Start에서 읽으므로 Awake에서 불러 둔다.
        private void Awake()
        {
            Nickname = PlayerPrefs.GetString(NicknameKey, "");
            if (Nickname.Length == 0)
            {
                Rename($"{firstNickname}{Random.Range(1000, 10000)}");
            }

            // 판도라는 에셋 이름으로 저장한다. 목록의 순서가 바뀌어도 같은 판도라를 찾는다.
            Pandora = PandoraNamed(PlayerPrefs.GetString(PandoraKey, firstPandora.name)) ?? firstPandora;
        }
    }
}
