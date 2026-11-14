using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>연습 상대 AI의 성향: 언제 치고, 몇 타까지 잇고, 얼마나 쉬고, 어떻게 피하는지. 정본은 Data/Rival의 에셋이다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Rival AI Tuning", fileName = "RivalAiTuning")]
    public sealed class RivalAiTuning : ScriptableObject
    {
        [Header("치기")]
        [Tooltip("상대와 이 거리(m, 몸 가운데끼리) 안이면 3연격을 시작한다")]
        [SerializeField] private float attackRange = 1.2f;
        [Tooltip("첫 타 뒤에 둘째 타를 이어 칠 확률(0~1)")]
        [SerializeField] private float secondHitChance = 0.8f;
        [Tooltip("둘째 타 뒤에 끝타를 이어 칠 확률(0~1)")]
        [SerializeField] private float thirdHitChance = 0.6f;
        [Tooltip("공격 버튼을 누르는 간격(초). 사람이 연타하는 빠르기")]
        [SerializeField] private float pressInterval = 0.22f;

        [Header("친 뒤")]
        [Tooltip("친 뒤 옆으로 돌 확률(0~1). 아니면 그 자리에서 숨을 고른다")]
        [SerializeField] private float circleChance = 0.5f;
        [Tooltip("숨 고르는 시간(초, 최소~최대)")]
        [SerializeField] private Vector2 waitTime = new Vector2(0.25f, 0.7f);
        [Tooltip("옆으로 도는 시간(초, 최소~최대)")]
        [SerializeField] private Vector2 circleTime = new Vector2(0.6f, 1.3f);
        [Tooltip("옆으로 돌 때 조이스틱을 미는 세기(0~1)")]
        [SerializeField] private float circleStrength = 0.45f;
        [Tooltip("옆으로 돌 때 지키려는 거리(m)")]
        [SerializeField] private float circleDistance = 2f;

        [Header("반응")]
        [Tooltip("상대가 공격을 시작한 것을 알아채기까지(초). 사람의 반응 시간")]
        [SerializeField] private float reactionTime = 0.2f;
        [Tooltip("상대가 이 거리(m) 안에서 공격을 시작하면 물러설지 따진다")]
        [SerializeField] private float backOffDistance = 1.8f;
        [Tooltip("그때 물러설 확률(0~1)")]
        [SerializeField] private float backOffChance = 0.4f;
        [Tooltip("물러서는 시간(초)")]
        [SerializeField] private float backOffTime = 0.35f;
        [Tooltip("맞은 뒤 휘청임이 끝나자마자 반격할 확률(0~1). 아니면 물러선다")]
        [SerializeField] private float counterChance = 0.35f;

        [Header("스킬")]
        [Tooltip("스킬을 쓸지 따지는 간격(초)")]
        [SerializeField] private float skillThinkInterval = 0.6f;
        [Tooltip("따질 때마다 쓸 확률(0~1). 쓸 수 있고 거리가 맞는 스킬을 매번 다른 칸부터 찾는다")]
        [SerializeField] private float skillChance = 0.45f;
        [Tooltip("차원이동으로 숨은 뒤 나오기까지(초)")]
        [SerializeField] private float gateHideTime = 0.8f;
        [Tooltip("차원이동으로 나올 때 상대 등 뒤로 떨어지는 거리(m)")]
        [SerializeField] private float gateExitGap = 1.2f;

        public float AttackRange => attackRange;
        public float SecondHitChance => secondHitChance;
        public float ThirdHitChance => thirdHitChance;
        public float PressInterval => pressInterval;

        public float CircleChance => circleChance;
        public Vector2 WaitTime => waitTime;
        public Vector2 CircleTime => circleTime;
        public float CircleStrength => circleStrength;
        public float CircleDistance => circleDistance;

        public float ReactionTime => reactionTime;
        public float BackOffDistance => backOffDistance;
        public float BackOffChance => backOffChance;
        public float BackOffTime => backOffTime;
        public float CounterChance => counterChance;

        public float SkillThinkInterval => skillThinkInterval;
        public float SkillChance => skillChance;
        public float GateHideTime => gateHideTime;
        public float GateExitGap => gateExitGap;
    }
}
