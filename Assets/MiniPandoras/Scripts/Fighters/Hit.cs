using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>한 번 맞힌 결과. 때린 쪽이 정해서 맞은 쪽에 넘기고, 화면(숫자·흔들림)도 이것을 받는다.</summary>
    public readonly struct Hit
    {
        public Hit(int damage, bool critical, bool finisher, Vector3 push, Vector3 source, bool staggers = true)
        {
            Damage = damage;
            Critical = critical;
            Finisher = finisher;
            Push = push;
            Source = source;
            Staggers = staggers;
        }

        public int Damage { get; }
        public bool Critical { get; }

        /// <summary>3연격의 마지막 타(크게 밀어내고 맞은 쪽이 크게 휘청인다).</summary>
        public bool Finisher { get; }

        /// <summary>맞은 쪽이 밀려나는(끌려오는) 바닥 방향과 거리(m).</summary>
        public Vector3 Push { get; }

        /// <summary>때린 쪽의 자리. 맞은 쪽은 이쪽을 보고 휘청인다(밀려나든 끌려오든 맞은 방향이 읽히게).</summary>
        public Vector3 Source { get; }

        /// <summary>
        /// 맞은 쪽이 휘청이며 하던 일을 놓치는지. 장판과 화상처럼 여러 번 나눠 들어오는 피해는 휘청이지 않는다
        /// (0.5초마다 휘청이면 장판 안에서 빠져나올 수 없다).
        /// </summary>
        public bool Staggers { get; }
    }
}
