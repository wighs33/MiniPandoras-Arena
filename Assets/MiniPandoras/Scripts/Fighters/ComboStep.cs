using System;
using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>3연격의 한 타. 시각은 모두 그 타를 시작한 때부터 잰 초다.</summary>
    [Serializable]
    public struct ComboStep
    {
        [Header("시각")]
        [Tooltip("다시 움직일 수 있게 되는 시각(초). 다음 타를 누르지 않았으면 이때 공격이 끝난다")]
        [SerializeField] private float duration;
        [Tooltip("미리 누른 다음 타가 이어지는 시각(초). 이보다 일찍 누른 것은 기억했다가 이때 잇는다")]
        [SerializeField] private float chainAt;
        [Tooltip("때리는 시각(초). 애니메이션에서 방망이가 가장 빠른 순간에 맞췄다")]
        [SerializeField] private float hitAt;

        [Header("내딛기")]
        [Tooltip("앞으로 내딛는 거리(m)")]
        [SerializeField] private float lunge;
        [Tooltip("내딛기가 끝나는 시각(초)")]
        [SerializeField] private float lungeTime;

        [Header("타격")]
        [Tooltip("몸에서 맞는 자리 가운데까지(m)")]
        [SerializeField] private float reach;
        [Tooltip("맞는 자리의 반지름(m)")]
        [SerializeField] private float radius;
        [Tooltip("피해 공식의 위력. 2D판은 1·2타 1, 3타 1.45다")]
        [SerializeField] private float power;
        [Tooltip("맞은 상대가 밀려나는 거리(m)")]
        [SerializeField] private float knock;

        public ComboStep(float duration, float chainAt, float hitAt, float lunge = 0f, float lungeTime = 0.1f,
            float reach = 1f, float radius = 0.8f, float power = 1f, float knock = 0.25f)
        {
            this.duration = duration;
            this.chainAt = chainAt;
            this.hitAt = hitAt;
            this.lunge = lunge;
            this.lungeTime = lungeTime;
            this.reach = reach;
            this.radius = radius;
            this.power = power;
            this.knock = knock;
        }

        public float Duration => duration;
        public float ChainAt => chainAt;
        public float HitAt => hitAt;
        public float Lunge => lunge;
        public float LungeTime => lungeTime;
        public float Reach => reach;
        public float Radius => radius;
        public float Power => power;
        public float Knock => knock;
    }
}
