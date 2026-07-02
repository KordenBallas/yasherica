using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformRimBuilderTests
    {
        private const float Size = 2f;
        private const float RimWidth = 1.2f;

        private static IReadOnlyList<(float X, float Z)> SingleCellOutline()
        {
            return HexOutlineExtractor.Extract(
                new[] { new HexCoordinates(0, 0) }, HexOrientation.Flat, Size, (0f, 0f));
        }

        [Test]
        public void RimRing_IsIndexAlignedWithSubdividedOutline()
        {
            var (subdivided, rim) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 35, new DeterministicRandom(3));

            Assert.AreEqual(subdivided.Count, rim.Count);
            Assert.AreEqual(12, subdivided.Count, "6 outline vertices + 6 midpoints");
        }

        [Test]
        public void RimVertices_LieOutsideTheOutline_WithinJitterBounds()
        {
            const int jitterPercent = 35;
            var (subdivided, rim) = PlatformRimBuilder.Build(
                SingleCellOutline(), RimWidth, jitterPercent, new DeterministicRandom(3));

            float minOffset = RimWidth * (1f - jitterPercent / 100f);
            // Tangential wobble can add up to 25% of the local segment length on top of the width.
            for (int i = 0; i < rim.Count; i++)
            {
                float radialBefore = Radius(subdivided[i]);
                float radialAfter = Radius(rim[i]);
                Assert.Greater(radialAfter, radialBefore, $"rim vertex {i} must move outward");

                float displacement = Distance(subdivided[i], rim[i]);
                Assert.GreaterOrEqual(displacement, minOffset - 1e-3f, $"vertex {i} under-displaced");
            }
        }

        [Test]
        public void ZeroJitter_OffsetsExactlyByRimWidth()
        {
            var (subdivided, rim) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 0, new DeterministicRandom(3));

            for (int i = 0; i < rim.Count; i++)
            {
                Assert.AreEqual(RimWidth, Distance(subdivided[i], rim[i]), 1e-3f, $"vertex {i}");
            }
        }

        [Test]
        public void SameSeed_ReplaysTheSameRim()
        {
            var (_, first) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 35, new DeterministicRandom(11));
            var (_, second) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 35, new DeterministicRandom(11));

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].X, second[i].X, 1e-6f);
                Assert.AreEqual(first[i].Z, second[i].Z, 1e-6f);
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentRims()
        {
            var (_, first) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 35, new DeterministicRandom(11));
            var (_, second) = PlatformRimBuilder.Build(SingleCellOutline(), RimWidth, 35, new DeterministicRandom(12));

            bool anyDifferent = false;
            for (int i = 0; i < first.Count && !anyDifferent; i++)
            {
                anyDifferent = System.Math.Abs(first[i].X - second[i].X) > 1e-6f
                    || System.Math.Abs(first[i].Z - second[i].Z) > 1e-6f;
            }

            Assert.IsTrue(anyDifferent);
        }

        private static float Radius((float X, float Z) p)
        {
            return (float)System.Math.Sqrt(p.X * p.X + p.Z * p.Z);
        }

        private static float Distance((float X, float Z) a, (float X, float Z) b)
        {
            float dx = b.X - a.X;
            float dz = b.Z - a.Z;
            return (float)System.Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
