using MiniPandoras.Skills;
using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 체력 막대 위의 이름표: 누구인지(「나」·「상대」, 온라인에서는 상대의 닉네임)와 지금 든 판도라를 그 판도라 색으로 보인다.
    /// 상대는 판마다 판도라가 바뀌므로, 바뀐 것을 보면 글을 다시 쓴다.
    /// </summary>
    public sealed class FighterNameView : MonoBehaviour
    {
        [SerializeField] private SkillCaster caster;
        [SerializeField] private TMP_Text label;
        [Tooltip("누구인지(「나」, 「상대」). 온라인 상대의 닉네임을 보이다가 혼자 하게 되면 이것으로 돌아온다")]
        [SerializeField] private string owner = "나";
        [Tooltip("판도라 이름 색을 흰색 쪽으로 밝히는 정도. 짙은 판 위에서 악마·차원 색이 읽히게 한다")]
        [Range(0f, 1f)]
        [SerializeField] private float nameLift = 0.35f;

        private string shownOwner;
        private PandoraDefinition shownPandora;
        private bool drawn;

        /// <summary>「상대」 대신 이 이름(온라인 상대의 닉네임)을 보인다.</summary>
        public void ShowName(string name)
        {
            shownOwner = name;
            drawn = false;
        }

        public void ShowDefaultName()
        {
            ShowName(owner);
        }

        private void Awake()
        {
            shownOwner = owner;
        }

        private void Update()
        {
            if (drawn && caster.Pandora == shownPandora)
            {
                return;
            }

            drawn = true;
            shownPandora = caster.Pandora;
            if (shownPandora == null)
            {
                label.text = shownOwner;
                return;
            }

            string color = ColorUtility.ToHtmlStringRGB(Color.Lerp(shownPandora.Color, Color.white, nameLift));
            label.text = $"{shownOwner} · <color=#{color}>{shownPandora.DisplayName}</color>";
        }
    }
}
