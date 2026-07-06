using System.Collections.Generic;
using NUnit.Framework;
using World.Dressing.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformEdgeFitTests
    {
        /// <summary>A 10×10 square outline centered on the origin (CCW).</summary>
        private static readonly List<(float X, float Z)> Square = new List<(float X, float Z)>
        {
            (-5f, -5f), (5f, -5f), (5f, 5f), (-5f, 5f)
        };

        [Test]
        public void SignedClearance_IsPositiveInside_NegativeOutside()
        {
            Assert.AreEqual(5f, PlatformEdgeFit.SignedClearance(Square, 0f, 0f), 1e-4f);
            Assert.AreEqual(1f, PlatformEdgeFit.SignedClearance(Square, 4f, 0f), 1e-4f);
            Assert.AreEqual(-2f, PlatformEdgeFit.SignedClearance(Square, 7f, 0f), 1e-4f);
        }

        [Test]
        public void TryFitInside_KeepsAFittingPointUntouched()
        {
            Assert.IsTrue(PlatformEdgeFit.TryFitInside(Square, 1f, 2f, 1.5f, out float x, out float z));
            Assert.AreEqual(1f, x, 1e-5f);
            Assert.AreEqual(2f, z, 1e-5f);
        }

        [Test]
        public void TryFitInside_NudgesAnOverhangingPointInward()
        {
            // 1 unit from the +X edge, needs 2 → must move to clearance ≥ 2.
            Assert.IsTrue(PlatformEdgeFit.TryFitInside(Square, 4f, 0f, 2f, out float x, out float z));
            Assert.GreaterOrEqual(PlatformEdgeFit.SignedClearance(Square, x, z), 2f - 1e-3f);
        }

        [Test]
        public void TryFitInside_PullsAnOutsidePointBackIn()
        {
            // Jittered past the edge entirely — the fix of the brief's core complaint.
            Assert.IsTrue(PlatformEdgeFit.TryFitInside(Square, 6f, 1f, 1f, out float x, out float z));
            Assert.GreaterOrEqual(PlatformEdgeFit.SignedClearance(Square, x, z), 1f - 1e-3f);
        }

        [Test]
        public void TryFitInside_FailsWhenThePropGenuinelyCannotFit()
        {
            // A 6-unit footprint cannot fit a 10×10 square (max clearance is 5).
            Assert.IsFalse(PlatformEdgeFit.TryFitInside(Square, 0f, 0f, 6f, out _, out _));
        }

        [Test]
        public void TryFitInside_IsDeterministic()
        {
            PlatformEdgeFit.TryFitInside(Square, 4.7f, -3.9f, 1.7f, out float x1, out float z1);
            PlatformEdgeFit.TryFitInside(Square, 4.7f, -3.9f, 1.7f, out float x2, out float z2);
            Assert.AreEqual(x1, x2);
            Assert.AreEqual(z1, z2);
        }
    }
}
