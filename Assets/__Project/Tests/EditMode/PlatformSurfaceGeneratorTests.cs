using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformSurfaceGeneratorTests
    {
        private const float Size = 2f;
        private const float RimWidth = 1.2f;
        private const int Jitter = 35;

        private static PlatformHexSurface Generate(
            ShapeProfile profile, int guaranteedMin = 0, ulong seed = 7,
            HexOrientation orientation = HexOrientation.Flat)
        {
            return new PlatformSurfaceGenerator().Generate(
                profile, guaranteedMin, Size, orientation, RimWidth, Jitter, new DeterministicRandom(seed));
        }

        [Test]
        public void SameSeed_ReplaysCellsOutlineAndRimExactly()
        {
            var profile = new ShapeProfile(12, 18, 6);
            var first = Generate(profile, guaranteedMin: 12, seed: 42);
            var second = Generate(profile, guaranteedMin: 12, seed: 42);

            CollectionAssert.AreEqual(
                first.Cells.Select(c => (c.Q, c.R)).ToList(),
                second.Cells.Select(c => (c.Q, c.R)).ToList());
            CollectionAssert.AreEqual(first.Outline, second.Outline);
            CollectionAssert.AreEqual(first.RimRing, second.RimRing);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentShapes()
        {
            var profile = new ShapeProfile(12, 18, 3);
            var first = Generate(profile, seed: 1);
            var second = Generate(profile, seed: 2);

            bool sameCells = first.Cells.Count == second.Cells.Count
                && first.Cells.Select(c => (c.Q, c.R)).SequenceEqual(second.Cells.Select(c => (c.Q, c.R)));
            Assert.IsFalse(sameCells);
        }

        [Test]
        public void CellCount_StaysWithinProfileRange_WhenNoHolesNeedFilling()
        {
            // Hole filling can only add cells, so the profile minimum is always honored; the maximum
            // may be exceeded only by filled holes (compact small blobs practically never enclose one,
            // but tolerate it rather than flake).
            var profile = new ShapeProfile(4, 7, 3);
            for (ulong seed = 0; seed < 30; seed++)
            {
                var surface = Generate(profile, seed: seed);
                Assert.GreaterOrEqual(surface.Cells.Count, 4, $"seed {seed}");
                Assert.LessOrEqual(surface.Cells.Count, 7 + 2, $"seed {seed} (hole-fill tolerance)");
            }
        }

        [Test]
        public void GuaranteedMinimum_OverridesASmallerProfile()
        {
            var lootLikeProfile = new ShapeProfile(3, 5, 3);
            for (ulong seed = 0; seed < 10; seed++)
            {
                var surface = Generate(lootLikeProfile, guaranteedMin: 12, seed: seed);
                Assert.GreaterOrEqual(surface.Cells.Count, 12, $"seed {seed}");
            }
        }

        [Test]
        public void Cells_AreAlwaysEdgeConnected()
        {
            var profile = new ShapeProfile(10, 16, 0); // raggedest growth
            for (ulong seed = 0; seed < 20; seed++)
            {
                var surface = Generate(profile, seed: seed);
                Assert.AreEqual(surface.Cells.Count, FloodCount(surface), $"seed {seed}");
            }
        }

        [Test]
        public void Interior_HasNoHoles()
        {
            // Every non-cell hex inside the bounding box must be reachable from outside the box.
            var profile = new ShapeProfile(12, 18, 0);
            for (ulong seed = 0; seed < 20; seed++)
            {
                var surface = Generate(profile, seed: seed);
                Assert.AreEqual(0, CountEnclosedHoles(surface), $"seed {seed}");
            }
        }

        [Test]
        public void HigherCompactness_YieldsRounderBlobs()
        {
            // Perimeter per cell (outline vertices / cell count) must average lower for compact growth.
            float raggedRatio = AveragePerimeterRatio(compactness: 0);
            float compactRatio = AveragePerimeterRatio(compactness: 8);
            Assert.Less(compactRatio, raggedRatio);
        }

        [Test]
        public void CenterCell_IsAMemberCell_AndNearOrigin()
        {
            var profile = new ShapeProfile(12, 18, 6);
            var surface = Generate(profile, seed: 5);

            Assert.IsTrue(surface.Contains(surface.CenterCell));
            var (x, z) = surface.GetCellCenterLocal(surface.CenterCell);
            // The centroid recenter puts the origin inside the blob, so the nearest cell center is
            // within one cell diameter of it.
            Assert.Less(System.Math.Sqrt(x * x + z * z), Size * 2f);
        }

        [Test]
        public void Centroid_IsRecenteredToTheOrigin()
        {
            var profile = new ShapeProfile(12, 18, 3);
            var surface = Generate(profile, seed: 9);

            float sumX = 0f, sumZ = 0f;
            foreach (var cell in surface.Cells)
            {
                var (x, z) = surface.GetCellCenterLocal(cell);
                sumX += x;
                sumZ += z;
            }

            Assert.AreEqual(0f, sumX / surface.Cells.Count, 1e-3f);
            Assert.AreEqual(0f, sumZ / surface.Cells.Count, 1e-3f);
        }

        [Test]
        public void SingleCellProfile_ProducesAValidSurface()
        {
            var surface = Generate(new ShapeProfile(1, 1, 0), seed: 3);

            Assert.AreEqual(1, surface.Cells.Count);
            Assert.AreEqual(6, surface.Outline.Count);
            Assert.AreEqual(surface.SubdividedOutline.Count, surface.RimRing.Count);
        }

        private static float AveragePerimeterRatio(int compactness)
        {
            var profile = new ShapeProfile(14, 14, compactness);
            float total = 0f;
            const int samples = 20;
            for (ulong seed = 100; seed < 100 + samples; seed++)
            {
                var surface = Generate(profile, seed: seed);
                total += surface.Outline.Count / (float)surface.Cells.Count;
            }

            return total / samples;
        }

        private static int FloodCount(PlatformHexSurface surface)
        {
            var visited = new HashSet<HexCoordinates> { surface.Cells[0] };
            var queue = new Queue<HexCoordinates>();
            queue.Enqueue(surface.Cells[0]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var next = current + offset;
                    if (surface.Contains(next) && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count;
        }

        private static int CountEnclosedHoles(PlatformHexSurface surface)
        {
            int minQ = surface.Cells.Min(c => c.Q) - 1;
            int maxQ = surface.Cells.Max(c => c.Q) + 1;
            int minR = surface.Cells.Min(c => c.R) - 1;
            int maxR = surface.Cells.Max(c => c.R) + 1;

            var outside = new HashSet<HexCoordinates>();
            var queue = new Queue<HexCoordinates>();
            var start = new HexCoordinates(minQ, minR);
            outside.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var next = current + offset;
                    if (next.Q < minQ || next.Q > maxQ || next.R < minR || next.R > maxR)
                    {
                        continue;
                    }

                    if (surface.Contains(next) || !outside.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            int holes = 0;
            for (int q = minQ; q <= maxQ; q++)
            {
                for (int r = minR; r <= maxR; r++)
                {
                    var hex = new HexCoordinates(q, r);
                    if (!surface.Contains(hex) && !outside.Contains(hex))
                    {
                        holes++;
                    }
                }
            }

            return holes;
        }
    }
}
