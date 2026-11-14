using System;
using UnityEngine;

namespace MiniPandoras.Skills
{
    /// <summary>스킬이 투기장에서 나가는 모양(2D판 SkillShape의 ShapeKind). 판도라를 더할 때 늘어난다.</summary>
    public enum SkillKind
    {
        /// <summary>곧게 날아가는 탄. 상대에게 닿으면 터진다.</summary>
        Projectile,
        /// <summary>한동안 바닥에 남아 여러 번 피해를 주는 장판(원이나 고리).</summary>
        Zone,
        /// <summary>상대에게 뛰어올라 내리꽂고, 그 자리에 장판을 남긴다.</summary>
        Leap,
        /// <summary>손을 뻗어 처음 닿은 상대를 몸 앞으로 끌어온다.</summary>
        Grab,
        /// <summary>상대 발밑에 깔리고 잠시 뒤 터진다.</summary>
        Trap,
        /// <summary>몸 둘레에서 바깥으로 가시가 겹겹이 번진다.</summary>
        Thorns,
        /// <summary>잠시 들어오는 공격을 흘려보낸다.</summary>
        Phase,
        /// <summary>바라보는 쪽으로 상대를 뚫고 내달리며, 지나친 상대를 한 번 벤다.</summary>
        Dash,
        /// <summary>상대 바로 뒤로 순간이동해 벤다.</summary>
        Warp,
        /// <summary>틈에 숨었다가 고른 자리로 나온다.</summary>
        Gate,
    }

    /// <summary>
    /// 모양별 수치. 2D판처럼 모양마다 쓰는 칸만 값이 있고 나머지는 0이다.
    /// 단위는 m와 초다(2D판 값을 16px = 1m, 60프레임 = 1초로 옮겼다. Docs/ARCHITECTURE.md의 「판도라 스킬」).
    /// </summary>
    [Serializable]
    public struct SkillShape
    {
        [SerializeField] private SkillKind kind;
        [Tooltip("Projectile·Grab: 탄(손)의 반지름, Zone: 장판의 반지름, Leap: 내리꽂는 자리의 반지름, Trap: 터지는 자리의 반지름, Thorns: 가장 바깥 겹까지 (m)")]
        [SerializeField] private float radius;
        [Tooltip("Thorns: 가시 하나가 닿는 반지름, Dash: 지나가며 닿는 반지름, Warp: 상대 뒤에 서는 간격 (m)")]
        [SerializeField] private float size;
        [Tooltip("Phase: 흘려보내는 시간, Gate: 나갈 곳을 고르는 시간 (초)")]
        [SerializeField] private float duration;
        [Tooltip("Phase: 공격을 흘리면 줄어드는 재사용 대기(초, 한 번)")]
        [SerializeField] private float refund;
        [Tooltip("Trap: 깔리고 터지기까지(초)")]
        [SerializeField] private float delay;
        [Tooltip("Zone: 쓴 사람을 따라다닌다")]
        [SerializeField] private bool follow;
        [Tooltip("Zone: 고리의 두께(m). 0이면 꽉 찬 원")]
        [SerializeField] private float band;
        [Tooltip("Zone: 몸에서 시작해 끝까지 퍼지는 시간(초). 0이면 바로 다 깔린다")]
        [SerializeField] private float growTime;
        [Tooltip("Projectile·Grab: 날아가는 빠르기(m/s)")]
        [SerializeField] private float speed;
        [Tooltip("Projectile·Grab: 사라질 때까지 날아가는 거리, Leap: 뛰는 거리(상대가 더 가까우면 상대 앞까지), Trap: 겨냥할 상대가 없을 때 앞으로 까는 거리, Dash: 내달리는 거리 (m)")]
        [SerializeField] private float range;
        [Tooltip("Leap·Dash: 날아가는(내달리는) 시간(초). 그동안 맞지 않는다")]
        [SerializeField] private float flightTime;
        [Tooltip("맞은 상대가 밀려나는 거리(m)")]
        [SerializeField] private float knock;
        [Tooltip("Zone, Leap의 장판: 피해를 주는 횟수, Thorns: 겹 수")]
        [SerializeField] private int ticks;
        [Tooltip("Zone, Leap의 장판: 피해 사이 간격, Thorns: 겹 사이 간격 (초)")]
        [SerializeField] private float interval;
        [Tooltip("Zone, Leap의 장판: 한 번 피해의 위력. 2D판은 스킬 위력과 같지만 1대1에서는 장판 피해가 한 사람에게 모여 줄였다")]
        [SerializeField] private float tickPower;
        [Tooltip("Leap: 내린 자리에 남기는 장판의 반지름(m)")]
        [SerializeField] private float puddleRadius;

        public SkillKind Kind => kind;
        public float Radius => radius;
        public float Size => size;
        public float Duration => duration;
        public float Refund => refund;
        public float Delay => delay;
        public bool Follow => follow;
        public float Band => band;
        public float GrowTime => growTime;
        public float Speed => speed;
        public float Range => range;
        public float FlightTime => flightTime;
        public float Knock => knock;
        public int Ticks => ticks;
        public float Interval => interval;
        public float TickPower => tickPower;
        public float PuddleRadius => puddleRadius;
    }
}
