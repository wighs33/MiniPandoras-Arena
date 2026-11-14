using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>인물의 몸놀림과 싸움 수치: 달리기, 3연격, 능력치, 맞았을 때. 정본은 Data/Fighters의 에셋이다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Fighter Tuning", fileName = "FighterTuning")]
    public sealed class FighterTuning : ScriptableObject
    {
        [Header("몸놀림")]
        [Tooltip("조이스틱을 끝까지 밀었을 때의 빠르기(m/s)")]
        [SerializeField] private float runSpeed = 3.6f;
        [Tooltip("빨라지는 정도(m/s²)")]
        [SerializeField] private float acceleration = 24f;
        [Tooltip("멈추는 정도(m/s²)")]
        [SerializeField] private float deceleration = 32f;
        [Tooltip("도는 빠르기(도/초)")]
        [SerializeField] private float turnSpeed = 900f;
        [Tooltip("떨어지는 가속도(m/s²)")]
        [SerializeField] private float gravity = 20f;
        [Tooltip("공격 3연격. 공격 버튼을 누를 때마다 다음 타로 이어진다")]
        [SerializeField] private ComboStep[] combo = Array.Empty<ComboStep>();
        [Tooltip("상대가 이 거리(m, 몸 가운데끼리) 안에서 앞에 있으면 공격하며 더 내딛지 않는다")]
        [SerializeField] private float lungeStopDistance = 0.95f;

        [Header("능력치 (2D판 주인공의 마지막 장 표)")]
        [SerializeField] private int maxHp = 212;
        [SerializeField] private int attack = 27;
        [SerializeField] private int defense = 17;
        [Tooltip("치명타 확률(0~1). 2D판은 모두 8%다")]
        [SerializeField] private float criticalChance = 0.08f;
        [Tooltip("최대 마나. 판을 시작할 때 가득 찬다")]
        [SerializeField] private int maxMana = 66;
        [Tooltip("마나가 1씩 차는 간격(초). 2D판의 18프레임")]
        [SerializeField] private float manaRegenInterval = 0.3f;
        [Tooltip("평타를 맞힐 때마다 얻는 마나")]
        [SerializeField] private int manaPerHit = 1;
        [Tooltip("스킬이 상대를 겨냥하는 거리(m). 더 멀면 바라보는 쪽으로 쓴다. 2D판의 130px")]
        [SerializeField] private float aimRange = 8.1f;

        [Header("맞았을 때")]
        [Tooltip("휘청이며 아무것도 못 하는 시간(초). 2D판 적의 14프레임")]
        [SerializeField] private float hitStun = 0.23f;
        [Tooltip("밀려나는 데 걸리는 시간(초). 2D판의 6프레임")]
        [SerializeField] private float knockTime = 0.1f;
        [Tooltip("맞은 순간 때린 쪽과 맞은 쪽이 함께 멈추는 시간(초). 2D판의 3프레임")]
        [SerializeField] private float hitStop = 0.05f;
        [Tooltip("치명타일 때 멈추는 시간(초). 2D판의 5프레임")]
        [SerializeField] private float criticalHitStop = 0.083f;
        [Tooltip("화상 피해를 받는 횟수. 2D판은 4번")]
        [SerializeField] private int burnTicks = 4;
        [Tooltip("화상 피해 사이 간격(초). 2D판의 30프레임")]
        [SerializeField] private float burnInterval = 0.5f;
        [Tooltip("느려졌을 때 빠르기 배율. 2D판은 절반")]
        [SerializeField] private float slowFactor = 0.5f;

        public float RunSpeed => runSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float TurnSpeed => turnSpeed;
        public float Gravity => gravity;
        public IReadOnlyList<ComboStep> Combo => combo;
        public float LungeStopDistance => lungeStopDistance;

        public int MaxHp => maxHp;
        public int Attack => attack;
        public int Defense => defense;
        public float CriticalChance => criticalChance;
        public int MaxMana => maxMana;
        public float ManaRegenInterval => manaRegenInterval;
        public int ManaPerHit => manaPerHit;
        public float AimRange => aimRange;

        public float HitStun => hitStun;
        public float KnockTime => knockTime;
        public float HitStop => hitStop;
        public float CriticalHitStop => criticalHitStop;
        public int BurnTicks => burnTicks;
        public float BurnInterval => burnInterval;
        public float SlowFactor => slowFactor;
    }
}
