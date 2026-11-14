using System;
using MiniPandoras.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 판도라 칸 하나: 판도라 색 보석, 이름, 스킬 넷의 이름. 누르면 <see cref="Picked"/>로 알리고, 고른 칸은 등불색 테를 두른다.
    /// </summary>
    public sealed class PandoraCard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image gem;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text skillNames;
        [Tooltip("고른 칸에만 보이는 테")]
        [SerializeField] private GameObject focus;
        [SerializeField] private CanvasGroup group;
        [Tooltip("고르지 않은 칸의 밝기(0~1)")]
        [SerializeField] private float idleAlpha = 0.7f;
        [Tooltip("판도라 이름 색을 흰색 쪽으로 밝히는 정도. 짙은 판 위에서 악마·차원 색이 읽히게 한다")]
        [Range(0f, 1f)]
        [SerializeField] private float nameLift = 0.35f;

        private PandoraDefinition shown;

        public event Action<PandoraDefinition> Picked;

        public void Show(PandoraDefinition pandora)
        {
            shown = pandora;
            gem.color = pandora.Color;
            title.color = Color.Lerp(pandora.Color, Color.white, nameLift);
            title.SetText(pandora.DisplayName);

            var names = new string[PandoraDefinition.SkillSlots];
            for (int slot = 0; slot < names.Length; slot++)
            {
                names[slot] = pandora.SkillAt(slot).DisplayName;
            }

            skillNames.SetText(string.Join("\n", names));
        }

        public void ShowSelected(bool selected)
        {
            focus.SetActive(selected);
            group.alpha = selected ? 1f : idleAlpha;
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            Picked?.Invoke(shown);
        }
    }
}
