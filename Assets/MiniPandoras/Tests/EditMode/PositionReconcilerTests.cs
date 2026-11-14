using MiniPandoras.Net;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>예측한 내 자리는 호스트의 답과 견줘 어긋난 만큼만, 한 번만, 나눠서 고친다.</summary>
    public sealed class PositionReconcilerTests
    {
        private static Vector3 X(float x)
        {
            return new Vector3(x, 0f, 0f);
        }

        [Test]
        public void LeavesSmallDifferencesAlone()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));

            reconciler.Reconcile(1, X(0.05f));

            Assert.AreEqual(Vector3.zero, reconciler.Pending, "틱 반쯤의 어긋남은 늘 생기므로 고치지 않는다");
        }

        [Test]
        public void SpreadsTheCorrectionOverFrames()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));
            reconciler.Reconcile(1, X(1f));

            Vector3 step = reconciler.TakeCorrection(0.0625f);

            Assert.AreEqual(0.5f, step.x, 1e-5f, "초당 8배로 옮기므로 1/16초에 절반");
            Assert.AreEqual(0.5f, reconciler.Pending.x, 1e-5f);
        }

        [Test]
        public void CorrectsTheSameMistakeOnlyOnce()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));
            reconciler.Record(2, X(0.1f));

            reconciler.Reconcile(1, X(1f));
            reconciler.Reconcile(2, X(1.1f));

            Assert.AreEqual(1f, reconciler.Pending.x, 1e-5f, "2번 입력의 자리도 이미 고친 셈이라 또 더하지 않는다");
        }

        [Test]
        public void CountsTheCorrectionStillOnItsWay()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));
            reconciler.Reconcile(1, X(1f));
            reconciler.TakeCorrection(0.0625f);

            // 절반만 옮긴 채 다음 입력을 보냈다. 남은 절반까지 옮긴 자리로 적혀야 한다.
            reconciler.Record(2, X(0.5f));
            reconciler.Reconcile(2, X(1f));

            Assert.AreEqual(0.5f, reconciler.Pending.x, 1e-5f);
        }

        [Test]
        public void SnapsWhenFarOff()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));
            reconciler.Reconcile(1, X(5f));

            Vector3 step = reconciler.TakeCorrection(0.016f);

            Assert.AreEqual(5f, step.x, 1e-5f, "순간이동만큼 어긋나면 한 번에 옮긴다");
            Assert.AreEqual(Vector3.zero, reconciler.Pending);
        }

        [Test]
        public void IgnoresInputsItNeverSentAndHeight()
        {
            var reconciler = new PositionReconciler(16);
            reconciler.Record(1, X(0f));

            reconciler.Reconcile(7, X(3f));
            reconciler.Reconcile(1, new Vector3(0f, 2f, 0f));

            Assert.AreEqual(Vector3.zero, reconciler.Pending);
        }
    }
}
