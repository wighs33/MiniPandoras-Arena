using MiniPandoras.Fighters;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>화상: 정해진 간격마다 같은 피해를 정해진 횟수만큼, 다시 붙으면 처음부터.</summary>
    public sealed class BurnTimerTests
    {
        [Test]
        public void BurnsAtEachIntervalForTheGivenTicks()
        {
            var burn = new BurnTimer();
            burn.Ignite(damage: 4, ticks: 2, tickInterval: 0.5f);

            Assert.AreEqual(0, burn.Tick(0.4f), "간격이 차기 전에는 태우지 않는다");
            Assert.AreEqual(4, burn.Tick(0.1f));
            Assert.AreEqual(4, burn.Tick(0.5f));
            Assert.IsFalse(burn.IsBurning);
            Assert.AreEqual(0, burn.Tick(0.5f), "횟수를 다 쓰면 꺼진다");
        }

        [Test]
        public void IgnitingAgainStartsOver()
        {
            var burn = new BurnTimer();
            burn.Ignite(damage: 4, ticks: 1, tickInterval: 0.5f);
            burn.Tick(0.4f);
            burn.Ignite(damage: 6, ticks: 1, tickInterval: 0.5f);

            Assert.AreEqual(0, burn.Tick(0.4f), "다시 붙으면 간격을 처음부터 센다");
            Assert.AreEqual(6, burn.Tick(0.1f));
        }

        [Test]
        public void ClearPutsTheFireOut()
        {
            var burn = new BurnTimer();
            burn.Ignite(damage: 4, ticks: 4, tickInterval: 0.5f);
            burn.Clear();

            Assert.IsFalse(burn.IsBurning);
            Assert.AreEqual(0, burn.Tick(1f));
        }
    }
}
