using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>체력 막대 아래의 얇은 마나 막대. 판도라가 없는 인물(아직 스킬이 없는 상대)에게는 보이지 않는다.</summary>
    public sealed class ManaBarView : MonoBehaviour
    {
        [SerializeField] private SkillCaster caster;
        [SerializeField] private RectTransform fill;
        [Tooltip("오른쪽 끝에 붙은 막대(상대). 켜면 왼쪽부터 줄어든다")]
        [SerializeField] private bool anchoredRight;

        private int shownMana = -1;

        private void Start()
        {
            gameObject.SetActive(caster.Pandora != null);
        }

        private void Update()
        {
            if (caster.Mana == shownMana)
            {
                return;
            }

            shownMana = caster.Mana;
            BarFill.Stretch(fill, (float)shownMana / caster.MaxMana, anchoredRight);
        }
    }
}
