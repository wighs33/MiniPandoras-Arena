using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>
    /// 스킬 하나. 이름·위력·마나·재사용 대기·모양은 2D판 스킬 에셋(Data/Battle/Skills)에서 옮겼고,
    /// 몸짓·그림·AI가 쓰는 거리는 아레나에서 정했다. 정본은 Data/Pandoras의 에셋이다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        [Header("쓰기")]
        [Tooltip("마나 소모")]
        [SerializeField] private int cost;
        [Tooltip("다시 쓸 때까지(초)")]
        [SerializeField] private float cooldown;
        [Tooltip("피해 공식의 위력. 장판은 한 번 피해마다 이 위력이다")]
        [SerializeField] private float power;
        [Tooltip("맞은 상대에게 붙는 화상: 최대 체력에서 한 번에 깎는 비율. 0이면 없다")]
        [SerializeField] private float burn;
        [Tooltip("맞은 상대가 느려지는 시간(초). 0이면 없다")]
        [SerializeField] private float slow;
        [SerializeField] private SkillShape shape;

        [Header("몸짓")]
        [SerializeField] private SkillPose pose;
        [Tooltip("다시 움직일 수 있게 되기까지(초)")]
        [SerializeField] private float castTime = 0.45f;
        [Tooltip("몸짓을 시작하고 기술이 나가는 시각(초). 그 전에 맞으면 나가지 않는다. Leap은 내리는 시각이다")]
        [SerializeField] private float releaseAt = 0.2f;

        [Header("그림")]
        [Tooltip("Projectile: 탄, Grab: 뻗는 손, Zone: 장판, Leap: 내린 자리에 남는 장판, Trap: 덫, Thorns: 가시 숲, " +
                 "Phase: 몸을 감싸는 기운, Dash: 몸에 붙는 꼬리, Warp: 떠나고 나타나는 자리의 틈, Gate: 나갈 곳 표시")]
        [SerializeField] private GameObject effect;
        [Tooltip("맞거나 내린 자리에서 터지는 효과(Phase: 흘릴 때). 없어도 된다")]
        [SerializeField] private GameObject impact;

        [Header("AI")]
        [Tooltip("AI가 상대와 이 거리(m, 최소~최대) 안에 있을 때 쓴다")]
        [SerializeField] private Vector2 aiRange = new Vector2(0f, 3f);

        public string DisplayName => displayName;
        public string Description => description;
        public int Cost => cost;
        public float Cooldown => cooldown;
        public float Power => power;
        public float Burn => burn;
        public float Slow => slow;
        public SkillShape Shape => shape;
        public SkillPose Pose => pose;
        public float CastTime => castTime;
        public float ReleaseAt => releaseAt;
        public GameObject Effect => effect;
        public GameObject Impact => impact;
        public Vector2 AiRange => aiRange;
    }
}
