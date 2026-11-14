using MiniPandoras.Fighters;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>3연격의 이어짐, 때림, 끝남. 시각은 데이터 그대로 쓰므로 프레임 길이와 상관없이 같은 결과가 나와야 한다.</summary>
    public sealed class ComboChainTests
    {
        private const float Frame = 1f / 30f;

        private static readonly ComboStep[] Steps =
        {
            new ComboStep(duration: 0.7f, chainAt: 0.25f, hitAt: 0.15f),
            new ComboStep(duration: 0.8f, chainAt: 0.3f, hitAt: 0.15f),
            new ComboStep(duration: 1.2f, chainAt: 9f, hitAt: 0.6f),
        };

        [Test]
        public void FirstPressStartsTheFirstStepAtOnce()
        {
            var combo = new ComboChain(Steps);

            Assert.IsTrue(combo.Press());
            Assert.AreEqual(0, combo.Step);
        }

        [Test]
        public void EarlyPressWaitsForChainTime()
        {
            var combo = new ComboChain(Steps);
            combo.Press();
            combo.Tick(0.1f);

            Assert.IsFalse(combo.Press(), "공격 중에 누르면 바로 시작하지 않고 기억한다");
            Assert.AreEqual(ComboChain.Change.Struck, combo.Tick(0.1f));
            Assert.AreEqual(ComboChain.Change.StepStarted, combo.Tick(0.1f));
            Assert.AreEqual(1, combo.Step);
        }

        [Test]
        public void EachStepStrikesOnceAtItsHitTime()
        {
            var combo = new ComboChain(Steps);
            combo.Press();

            Assert.AreEqual(ComboChain.Change.None, combo.Tick(0.1f));
            Assert.AreEqual(ComboChain.Change.Struck, combo.Tick(0.1f));
            Assert.AreEqual(ComboChain.Change.None, combo.Tick(0.1f), "한 타는 한 번만 때린다");
        }

        [Test]
        public void TheNextStepNeverStartsBeforeThisOneStrikes()
        {
            var combo = new ComboChain(Steps);
            combo.Press();
            combo.Press();

            Assert.AreEqual(ComboChain.Change.Struck, combo.Tick(0.4f), "이어지는 시각을 넘겼어도 먼저 때린다");
            Assert.AreEqual(ComboChain.Change.StepStarted, combo.Tick(Frame));
        }

        [Test]
        public void ThreePressesReachTheLastStepAndThenFinish()
        {
            var combo = new ComboChain(Steps);
            combo.Press();
            combo.Press();
            Run(combo, 0.3f);
            combo.Press();
            combo.Press();
            Run(combo, 0.35f);

            Assert.AreEqual(2, combo.Step);
            Assert.IsTrue(combo.IsFinisher);
            Assert.IsFalse(combo.Press(), "마지막 타 뒤에는 더 잇지 않는다");
            Run(combo, 1.3f);
            Assert.IsFalse(combo.IsAttacking);
        }

        [Test]
        public void WithoutAnotherPressTheStepEndsAtItsDuration()
        {
            var combo = new ComboChain(Steps);
            combo.Press();

            Run(combo, 0.65f);
            Assert.IsTrue(combo.IsAttacking);
            Assert.AreEqual(ComboChain.Change.Finished, combo.Tick(0.1f));
            Assert.AreEqual(-1, combo.Step);
        }

        [Test]
        public void CancelForgetsTheQueuedStep()
        {
            var combo = new ComboChain(Steps);
            combo.Press();
            combo.Press();
            combo.Cancel();

            Assert.IsFalse(combo.IsAttacking);
            Assert.AreEqual(ComboChain.Change.None, combo.Tick(0.4f));
            Assert.IsTrue(combo.Press(), "끊긴 뒤에는 첫 타부터 다시 시작한다");
            Assert.AreEqual(0, combo.Step);
        }

        private static void Run(ComboChain combo, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                combo.Tick(Frame);
            }
        }
    }
}
