using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Ranking
{
    /// <summary>순위표의 줄 하나를 그린다: 순위, 판도라 보석, 닉네임, 판도라 이름, 점수.</summary>
    public sealed class RankingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rank;
        [SerializeField] private Image gem;
        [SerializeField] private TMP_Text nickname;
        [SerializeField] private TMP_Text pandoraName;
        [SerializeField] private TMP_Text score;
        [Tooltip("줄 바탕. 줄마다 번갈아 옅게 깔아 긴 목록에서 눈이 줄을 따라가게 한다")]
        [SerializeField] private Image background;
        [Tooltip("판도라 이름 색을 흰색 쪽으로 밝히는 정도. 짙은 판 위에서 악마·차원 색이 읽히게 한다")]
        [Range(0f, 1f)]
        [SerializeField] private float nameLift = 0.35f;

        public void Show(RankingEntry entry, bool shaded)
        {
            rank.SetText("{0}", entry.Rank);
            nickname.text = entry.Nickname;
            score.SetText("{0}", entry.Score);
            background.enabled = shaded;

            bool knownPandora = entry.Pandora != null;
            gem.enabled = knownPandora;
            pandoraName.text = knownPandora ? entry.Pandora.DisplayName : "";
            if (knownPandora)
            {
                gem.color = entry.Pandora.Color;
                pandoraName.color = Color.Lerp(entry.Pandora.Color, Color.white, nameLift);
            }
        }
    }
}
