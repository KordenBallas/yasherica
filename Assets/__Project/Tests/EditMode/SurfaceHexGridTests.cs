using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class SurfaceHexGridTests
    {
        private static readonly Vector3 Center = new Vector3(25f, 3f, -4f);

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(12, 18, 6), guaranteedMinCells: 12, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static SurfaceHexGrid CreateGrid(PlatformHexSurface surface)
        {
            var grid = new SurfaceHexGrid();
            grid.Initialize(surface, Center);
            return grid;
        }

        [Test]
        public void GridCells_AreExactlyTheSurfaceCells()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            var cells = grid.GetCellsInBoundary();
            Assert.AreEqual(surface.Cells.Count, cells.Count);
            foreach (var cell in cells)
            {
                Assert.IsTrue(surface.Contains(cell));
            }
        }

        [Test]
        public void HexToWorld_MatchesSurfaceCellCenters()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            foreach (var cell in surface.Cells)
            {
                var (x, z) = surface.GetCellCenterLocal(cell);
                Vector3 world = grid.HexToWorld(cell);
                Assert.AreEqual(Center.x + x, world.x, 1e-4f);
                Assert.AreEqual(Center.z + z, world.z, 1e-4f);
            }
        }

        [Test]
        public void WorldToHex_RoundTripsEveryCell()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            foreach (var cell in surface.Cells)
            {
                Assert.AreEqual(cell, grid.WorldToHex(grid.HexToWorld(cell)));
            }
        }

        [Test]
        public void IsCellInBoundary_IsSetMembership_NoPolygonTest()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            foreach (var cell in surface.Cells)
            {
                Assert.IsTrue(grid.IsCellInBoundary(cell));
            }

            // A far-away coordinate is never in.
            Assert.IsFalse(grid.IsCellInBoundary(new HexCoordinates(100, 100)));
        }

        [Test]
        public void GridMetrics_RideOnTheSurface()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            Assert.AreEqual(surface.HexSize, grid.HexSize);
            Assert.AreEqual(surface.Orientation, grid.Orientation);
            Assert.AreEqual(surface.Outline.Count, grid.Boundary.Count);
        }

        [Test]
        public void GetCellPosition_IsTheLocalOffset()
        {
            var surface = GenerateSurface();
            var grid = CreateGrid(surface);

            foreach (var cell in surface.Cells)
            {
                Vector3 local = grid.GetCellPosition(cell);
                Vector3 world = grid.HexToWorld(cell);
                Assert.AreEqual(world.x, Center.x + local.x, 1e-4f);
                Assert.AreEqual(world.z, Center.z + local.z, 1e-4f);
            }
        }
    }
}
