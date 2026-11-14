using MiniPandoras.Net;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>참가자 화면의 시계는 고르게 흐르면서 「받은 가장 새 소식 − 늦춤」을 천천히 따라간다.</summary>
    public sealed class PlaybackClockTests
    {
        private const double Delay = 0.1;

        [Test]
        public void StartsAtTheFirstNewsMinusTheDelay()
        {
            var clock = new PlaybackClock();
            clock.Received(10.0);

            clock.Advance(0.016, Delay);

            Assert.AreEqual(9.9, clock.Time, 1e-9);
        }

        [Test]
        public void FlowsInRealTimeWhenOnSchedule()
        {
            var clock = new PlaybackClock();
            clock.Received(10.0);
            clock.Advance(0.0, Delay);

            clock.Received(10.05);
            clock.Advance(0.05, Delay);

            Assert.AreEqual(9.95, clock.Time, 1e-9);
        }

        [Test]
        public void CatchesUpAtMostTenPercentFaster()
        {
            var clock = new PlaybackClock();
            clock.Received(10.0);
            clock.Advance(0.0, Delay);

            // 소식이 0.3초 앞서 쌓였다: 시계는 1.1배까지만 빨라진다.
            clock.Received(10.3);
            clock.Advance(0.1, Delay);

            Assert.AreEqual(9.9 + 0.11, clock.Time, 1e-9);
        }

        [Test]
        public void SlowsDownWhenNewsStopsComing()
        {
            var clock = new PlaybackClock();
            clock.Received(10.0);
            clock.Advance(0.0, Delay);

            clock.Advance(0.2, Delay);

            Assert.Less(clock.Time, 9.9 + 0.2, "새 소식이 없으면 앞서 나간 만큼 느려진다");
            Assert.GreaterOrEqual(clock.Time, 9.9 + 0.2 * 0.9 - 1e-9, "그래도 0.9배보다 느려지지는 않는다");
        }

        [Test]
        public void SnapsWhenFarBehind()
        {
            var clock = new PlaybackClock();
            clock.Received(10.0);
            clock.Advance(0.0, Delay);

            clock.Received(12.0);
            clock.Advance(0.016, Delay);

            Assert.AreEqual(11.9, clock.Time, 1e-9, "0.5초 넘게 뒤처지면 바로 맞춘다");
        }

        [Test]
        public void StandsStillBeforeAnyNews()
        {
            var clock = new PlaybackClock();

            clock.Advance(1.0, Delay);

            Assert.IsFalse(clock.HasStarted);
            Assert.AreEqual(0.0, clock.Time);
        }
    }
}
