using MiniPandoras.Net;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>참가자 화면은 받은 모습 사이를 메워 그리고, 받은 범위 밖은 짐작하지 않는다.</summary>
    public sealed class SnapshotBufferTests
    {
        private static FighterSnapshot At(float x, float yaw = 0f)
        {
            return new FighterSnapshot { Position = new Vector3(x, 0f, 0f), Yaw = yaw, Speed = x };
        }

        [Test]
        public void BlendsBetweenTheTwoSnapshotsAroundTheTime()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(1.0, At(0f));
            buffer.Add(1.1, At(1f));

            Assert.IsTrue(buffer.TrySample(1.025, out FighterSnapshot pose));
            Assert.AreEqual(0.25f, pose.Position.x, 0.0001f);
            Assert.AreEqual(0.25f, pose.Speed, 0.0001f);
        }

        [Test]
        public void HoldsTheEdgesInsteadOfGuessing()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(1.0, At(0f));
            buffer.Add(1.1, At(1f));

            buffer.TrySample(0.5, out FighterSnapshot early);
            buffer.TrySample(2.0, out FighterSnapshot late);

            Assert.AreEqual(0f, early.Position.x, "받은 것보다 이르면 가장 이른 모습");
            Assert.AreEqual(1f, late.Position.x, "받은 것보다 늦으면 가장 새 모습(앞질러 짐작하지 않는다)");
        }

        [Test]
        public void IgnoresSnapshotsThatArriveOutOfOrder()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(1.0, At(0f));
            buffer.Add(1.2, At(2f));
            buffer.Add(1.1, At(99f));

            buffer.TrySample(1.1, out FighterSnapshot pose);

            Assert.AreEqual(1f, pose.Position.x, 0.0001f, "늦게 온 1.1초 모습은 버리고 1.0초와 1.2초 사이를 메운다");
            Assert.AreEqual(2, buffer.Count);
        }

        [Test]
        public void KeepsOnlyTheNewestWhenFull()
        {
            var buffer = new SnapshotBuffer(3);
            for (int i = 0; i < 5; i++)
            {
                buffer.Add(i, At(i));
            }

            buffer.TrySample(0.0, out FighterSnapshot oldest);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(2f, oldest.Position.x, "가장 오래된 둘은 밀려났다");
        }

        [Test]
        public void DoesNotSlideThroughATeleport()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(1.0, At(0f));
            buffer.Add(1.1, At(5f));

            buffer.TrySample(1.09, out FighterSnapshot pose);

            Assert.AreEqual(0f, pose.Position.x, "순간이동은 사이를 메우지 않는다");
        }

        [Test]
        public void TurnsTheShortWayAroundAFullCircle()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(1.0, At(0f, 350f));
            buffer.Add(1.1, At(0f, 10f));

            buffer.TrySample(1.05, out FighterSnapshot pose);

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, pose.Yaw), 0.001f, "350도에서 10도로는 0도를 지나 돈다");
        }

        [Test]
        public void ReportsNothingBeforeTheFirstSnapshot()
        {
            var buffer = new SnapshotBuffer(8);

            Assert.IsFalse(buffer.TrySample(1.0, out _));
        }
    }
}
