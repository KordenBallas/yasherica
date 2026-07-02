using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration.Surface;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HexOutlineExtractorTests
    {
        private const float Size = 2f;

        [Test]
        public void SingleCell_YieldsItsSixCorners()
        {
            var outline = Extract(new HexCoordinates(0, 0));

            Assert.AreEqual(6, outline.Count);
            foreach (var (x, z) in outline)
            {
                Assert.AreEqual(Size, System.Math.Sqrt(x * x + z * z), 1e-3f);
            }
        }

        [Test]
        public void TwoAdjacentCells_YieldTenVertices()
        {
            // 6 + 6 corners minus the 2 shared by the swallowed edge.
            var outline = Extract(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
            Assert.AreEqual(10, outline.Count);
        }

        [Test]
        public void Outline_IsCounterClockwise()
        {
            foreach (var orientation in new[] { HexOrientation.Flat, HexOrientation.Pointy })
            {
                var outline = HexOutlineExtractor.Extract(
                    new[] { new HexCoordinates(0, 0), new HexCoordinates(1, 0), new HexCoordinates(0, 1) },
                    orientation, Size, (0f, 0f));

                Assert.Greater(SignedArea(outline), 0f,
                    $"interior must lie on the left of the walk (CCW), {orientation}");
            }
        }

        [Test]
        public void Outline_HasNoDuplicateVertices()
        {
            var outline = Extract(
                new HexCoordinates(0, 0), new HexCoordinates(1, 0), new HexCoordinates(0, 1),
                new HexCoordinates(1, -1));

            var seen = new HashSet<(long, long)>();
            foreach (var (x, z) in outline)
            {
                Assert.IsTrue(seen.Add(((long)System.Math.Round(x * 10000f), (long)System.Math.Round(z * 10000f))),
                    $"duplicate vertex at ({x}, {z})");
            }
        }

        [Test]
        public void CenterOffset_ShiftsEveryVertex()
        {
            var raw = Extract(new HexCoordinates(0, 0));
            var shifted = HexOutlineExtractor.Extract(
                new[] { new HexCoordinates(0, 0) }, HexOrientation.Flat, Size, (1.5f, -0.5f));

            for (int i = 0; i < raw.Count; i++)
            {
                Assert.AreEqual(raw[i].X - 1.5f, shifted[i].X, 1e-4f);
                Assert.AreEqual(raw[i].Z + 0.5f, shifted[i].Z, 1e-4f);
            }
        }

        [Test]
        public void SegmentCount_MatchesBorderEdges()
        {
            // Two adjacent cells: 12 edges total, 2 swallowed => 10 border segments = 10 vertices.
            // A triangle of three mutually adjacent cells: 18 - 6 swallowed = 12.
            var outline = Extract(new HexCoordinates(0, 0), new HexCoordinates(1, 0), new HexCoordinates(0, 1));
            Assert.AreEqual(12, outline.Count);
        }

        private static IReadOnlyList<(float X, float Z)> Extract(params HexCoordinates[] cells)
        {
            return HexOutlineExtractor.Extract(cells, HexOrientation.Flat, Size, (0f, 0f));
        }

        private static float SignedArea(IReadOnlyList<(float X, float Z)> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.X * b.Z - b.X * a.Z;
            }

            return area * 0.5f;
        }
    }
}
