using System;
using Combat.Battlefield;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HexMetricsTests
    {
        private const float Size = 2f;
        private const float Sqrt3 = 1.7320508f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void FlatCellCenter_MatchesLegacyFlatHexGridFormula()
        {
            // Legacy FlatHexGrid.HexToWorld fallback: x = size * 1.5 * Q; z = size * sqrt3 * (R + Q/2).
            AssertCenter(new HexCoordinates(0, 0), HexOrientation.Flat, 0f, 0f);
            AssertCenter(new HexCoordinates(1, 0), HexOrientation.Flat, 3f, Sqrt3);
            AssertCenter(new HexCoordinates(0, 1), HexOrientation.Flat, 0f, 2f * Sqrt3);
            AssertCenter(new HexCoordinates(1, -1), HexOrientation.Flat, 3f, -Sqrt3);
            AssertCenter(new HexCoordinates(-2, 1), HexOrientation.Flat, -6f, 0f);
        }

        [Test]
        public void PointyCellCenter_MatchesLegacyPointyHexGridFormula()
        {
            // Legacy PointyHexGrid.HexToWorld fallback: x = size * (sqrt3*Q + sqrt3/2*R); z = size * 1.5 * R.
            AssertCenter(new HexCoordinates(0, 0), HexOrientation.Pointy, 0f, 0f);
            AssertCenter(new HexCoordinates(1, 0), HexOrientation.Pointy, 2f * Sqrt3, 0f);
            AssertCenter(new HexCoordinates(0, 1), HexOrientation.Pointy, Sqrt3, 3f);
            AssertCenter(new HexCoordinates(1, -1), HexOrientation.Pointy, Sqrt3, -3f);
        }

        private static readonly HexOrientation[] BothOrientations =
        {
            HexOrientation.Flat, HexOrientation.Pointy
        };

        [Test]
        public void NeighborCenters_AreOneCellApart()
        {
            // Adjacent hex centers sit sqrt3 * size apart for both orientations.
            foreach (var orientation in BothOrientations)
            {
                var origin = HexMetrics.CellCenter(new HexCoordinates(0, 0), orientation, Size);
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var neighbor = HexMetrics.CellCenter(offset, orientation, Size);
                    float distance = Distance(origin, neighbor);
                    Assert.AreEqual(Sqrt3 * Size, distance, Tolerance, $"offset ({offset.Q},{offset.R}) {orientation}");
                }
            }
        }

        [Test]
        public void EdgeMidpoints_AlignWithNeighborOffsets()
        {
            // Edge i (between corners i and i+1) must border the neighbor at NeighborOffsets[i]: the
            // edge midpoint is half-way to that neighbor's center. This is the contract the outline
            // extractor is built on.
            foreach (var orientation in BothOrientations)
            {
                for (int edge = 0; edge < HexMetrics.CornerCount; edge++)
                {
                    var a = HexMetrics.Corner(edge, orientation, Size);
                    var b = HexMetrics.Corner((edge + 1) % HexMetrics.CornerCount, orientation, Size);
                    var midpoint = ((a.X + b.X) * 0.5f, (a.Z + b.Z) * 0.5f);

                    var neighborCenter = HexMetrics.CellCenter(HexMetrics.NeighborOffsets[edge], orientation, Size);
                    var halfway = (neighborCenter.X * 0.5f, neighborCenter.Z * 0.5f);

                    Assert.AreEqual(halfway.Item1, midpoint.Item1, Tolerance, $"edge {edge} X ({orientation})");
                    Assert.AreEqual(halfway.Item2, midpoint.Item2, Tolerance, $"edge {edge} Z ({orientation})");
                }
            }
        }

        [Test]
        public void Corners_LieOnTheCellRadius()
        {
            foreach (var orientation in BothOrientations)
            {
                for (int i = 0; i < HexMetrics.CornerCount; i++)
                {
                    var corner = HexMetrics.Corner(i, orientation, Size);
                    Assert.AreEqual(Size, (float)Math.Sqrt(corner.X * corner.X + corner.Z * corner.Z), Tolerance);
                }
            }
        }

        [Test]
        public void RoundTrip_CenterToFractionalToCell_ReturnsTheSameCell()
        {
            foreach (var orientation in BothOrientations)
            {
                for (int q = -4; q <= 4; q++)
                {
                    for (int r = -4; r <= 4; r++)
                    {
                        var hex = new HexCoordinates(q, r);
                        var (x, z) = HexMetrics.CellCenter(hex, orientation, Size);
                        var (fq, fr) = HexMetrics.LocalToFractional(x, z, orientation, Size);
                        var rounded = HexMetrics.Round(fq, fr);
                        Assert.AreEqual(hex, rounded, $"({q},{r}) {orientation}");
                    }
                }
            }
        }

        [Test]
        public void Round_NearCenterOffsets_SnapToTheCell()
        {
            // A point well inside a cell (under half the inradius off-center) must round to it.
            var hex = new HexCoordinates(2, -1);
            var (x, z) = HexMetrics.CellCenter(hex, HexOrientation.Flat, Size);
            var (fq, fr) = HexMetrics.LocalToFractional(x + 0.4f, z - 0.4f, HexOrientation.Flat, Size);
            Assert.AreEqual(hex, HexMetrics.Round(fq, fr));
        }

        private static void AssertCenter(HexCoordinates hex, HexOrientation orientation, float expectedX, float expectedZ)
        {
            var (x, z) = HexMetrics.CellCenter(hex, orientation, Size);
            Assert.AreEqual(expectedX, x, Tolerance, $"({hex.Q},{hex.R}) X");
            Assert.AreEqual(expectedZ, z, Tolerance, $"({hex.Q},{hex.R}) Z");
        }

        private static float Distance((float X, float Z) a, (float X, float Z) b)
        {
            float dx = b.X - a.X;
            float dz = b.Z - a.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
