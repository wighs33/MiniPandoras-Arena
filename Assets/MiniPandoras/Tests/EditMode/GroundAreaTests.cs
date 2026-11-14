using MiniPandoras.Combat;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>공격이 닿는 자리는 바닥 평면에서만 잰다.</summary>
    public sealed class GroundAreaTests
    {
        [Test]
        public void BodyRadiusCountsTowardTheReach()
        {
            Vector3 center = Vector3.zero;

            Assert.IsTrue(GroundArea.InCircle(center, 1f, new Vector3(1.25f, 0f, 0f), 0.3f), "몸 가장자리가 원에 걸치면 닿는다");
            Assert.IsFalse(GroundArea.InCircle(center, 1f, new Vector3(1.35f, 0f, 0f), 0.3f));
        }

        [Test]
        public void HeightIsIgnored()
        {
            Assert.IsTrue(GroundArea.InCircle(Vector3.zero, 1f, new Vector3(0.5f, 3f, 0f), 0.3f));
        }

        [Test]
        public void DirectionFallsBackWhenBodiesOverlap()
        {
            Vector3 fallback = Vector3.forward;

            Assert.AreEqual(Vector3.right, GroundArea.Direction(Vector3.zero, new Vector3(2f, 1f, 0f), fallback));
            Assert.AreEqual(fallback, GroundArea.Direction(Vector3.one, Vector3.one, fallback));
        }
    }
}
