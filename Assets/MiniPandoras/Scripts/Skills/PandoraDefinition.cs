using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>인물이 들고 싸우는 판도라 하나: 이름, 색, 스킬 넷(Q, E, R, 궁극기 T). 정본은 Data/Pandoras의 에셋이다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Pandora")]
    public sealed class PandoraDefinition : ScriptableObject
    {
        /// <summary>스킬 칸 수(Q, E, R, 궁극기).</summary>
        public const int SkillSlots = 4;

        [SerializeField] private string displayName;
        [Tooltip("스킬 버튼 테두리처럼 이 판도라를 드러내는 색")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("Q, E, R, 궁극기 순서")]
        [SerializeField] private SkillDefinition[] skills = new SkillDefinition[SkillSlots];

        public string DisplayName => displayName;
        public Color Color => color;

        /// <summary>그 칸(0부터)의 스킬. 칸을 벗어나거나 비었으면 null.</summary>
        public SkillDefinition SkillAt(int slot)
        {
            return slot >= 0 && slot < skills.Length ? skills[slot] : null;
        }
    }
}
