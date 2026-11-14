using MiniPandoras.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면의 스킬 버튼 하나. 스킬 이름을 쓰고, 테두리를 판도라 색으로 칠하고, 다시 쓸 때까지 어두운 부채꼴로 덮고,
    /// 마나가 모자라면 옅게 한다. 든 판도라가 바뀌면 이름과 색을 다시 그린다.
    /// 누르는 것은 같은 오브젝트의 OnScreenButton이 게임패드 단추처럼 입력으로 넘긴다.
    /// </summary>
    public sealed class SkillButtonView : MonoBehaviour
    {
        [SerializeField] private SkillCaster caster;
        [Tooltip("스킬 칸(0부터: Q, E, R, 궁극기 T)")]
        [SerializeField] private int slot;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Graphic ring;
        [Tooltip("다시 쓸 때까지 덮는 어두운 부채꼴(Image Type Filled, Radial 360)")]
        [SerializeField] private Image cooldownCover;
        [SerializeField] private CanvasGroup group;
        [Tooltip("마나가 모자랄 때의 밝기(0~1)")]
        [SerializeField] private float lackingManaAlpha = 0.45f;

        private PandoraDefinition shown;

        private void Update()
        {
            if (caster.Pandora != shown)
            {
                Redraw();
            }

            cooldownCover.fillAmount = caster.CooldownRatio(slot);
            group.alpha = caster.HasManaFor(slot) ? 1f : lackingManaAlpha;
        }

        private void Redraw()
        {
            shown = caster.Pandora;
            SkillDefinition skill = caster.SkillAt(slot);
            nameLabel.SetText(skill != null ? skill.DisplayName : "");
            ring.color = shown != null ? shown.Color : Color.clear;
        }
    }
}
