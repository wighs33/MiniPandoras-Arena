using System;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 참가자 화면이 지금 그리는 호스트 시각. 실제 시간대로 흐르되, 「받은 가장 새 소식의 시각 − 늦춤」을 향해 조금씩 당겨진다.
    /// 소식이 고르지 않게 와도(지터) 그리는 시각은 고르게 흐르고, 늦어지거나 앞서면 천천히 따라잡는다.
    /// Netcode가 짐작한 호스트 시계(ServerTime)는 받은 소식보다 평균 0.08초 뒤처지고 크게 흔들려서
    /// (지연 50ms·흔들림 20ms에서 잰 값), 받은 소식의 시각만으로 맞춘다.
    /// </summary>
    public sealed class PlaybackClock
    {
        // 이보다 어긋나면 천천히 맞추지 않고 바로 맞춘다(처음 받았을 때, 오래 멈췄다 돌아왔을 때).
        private const double SnapError = 0.5;
        // 맞춰 가는 동안 시계를 이만큼까지만 빠르게·느리게 돌린다. 더 크면 인물이 눈에 띄게 빨라지거나 느려진다.
        private const double MaxRateChange = 0.1;
        // 어긋난 시간(초)에 이 값을 곱한 비율만큼 빠르게·느리게 돈다.
        private const double CatchUpGain = 2.0;

        private double newest = double.NegativeInfinity;

        /// <summary>지금 그리는 호스트 시각(초). 아무것도 받기 전에는 의미가 없다.</summary>
        public double Time { get; private set; }

        public bool HasStarted => !double.IsNegativeInfinity(newest);

        /// <summary>호스트가 그 시각에 보낸 소식을 받았다.</summary>
        public void Received(double hostTime)
        {
            newest = Math.Max(newest, hostTime);
        }

        /// <param name="deltaTime">지난 프레임부터 흐른 실제 시간(초, 느린 화면 연출과 상관없이)</param>
        /// <param name="delay">받은 가장 새 소식보다 얼마나 늦게 그릴지(초)</param>
        public void Advance(double deltaTime, double delay)
        {
            if (!HasStarted)
            {
                return;
            }

            Time += deltaTime;
            double target = newest - delay;
            double error = target - Time;
            if (Math.Abs(error) > SnapError)
            {
                Time = target;
                return;
            }

            double rateChange = Math.Max(-MaxRateChange, Math.Min(MaxRateChange, error * CatchUpGain));
            Time += deltaTime * rateChange;
        }
    }
}
