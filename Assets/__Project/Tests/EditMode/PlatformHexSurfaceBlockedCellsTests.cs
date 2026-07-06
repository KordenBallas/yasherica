using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The environment-dressing BlockedCells seam: blocked cells stay part of the ground (mesh,
    /// Contains) but leave the combat grid and the landing anchors.
    /// </summary>
    [TestFixture]
    public class PlatformHexSurfaceBlockedCellsTests
    {
        private static readonly Vector3 Center = new Vector3(10f, 2f, -3f);

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(12, 18, 6), guaranteedMinCells: 12, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static HexCoordinates PickNonCenterCell(PlatformHexSurface surface)
        {
            foreach (var cell in surface.Cells)
            {
                if (!cell.Equals(surface.CenterCell))
                {
                    return cell;
                }
            }

            Assert.Fail("Surface has only the center cell.");
            return default;
        }

        [Test]
        public void FreshSurface_HasNoBlockedCells()
        {
            var surface = GenerateSurface();
            Assert.AreEqual(0, surface.BlockedCells.Count);
            foreach (var cell in surface.Cells)
            {
                Assert.IsFalse(surface.IsBlocked(cell));
            }
        }

        [Test]
        public void WithBlockedCells_PreservesEverythingElse()
        {
            var surface = GenerateSurface();
            var blockedCell = PickNonCenterCell(surface);
            var blocked = surface.WithBlockedCells(new[] { blockedCell });

            Assert.AreEqual(surface.Cells.Count, blocked.Cells.Count);
            Assert.AreEqual(surface.Outline.Count, blocked.Outline.Count);
            Assert.AreEqual(surface.RimRing.Count, blocked.RimRing.Count);
            Assert.AreEqual(surface.NotchFills.Count, blocked.NotchFills.Count);
            Assert.AreEqual(surface.CenterCell, blocked.CenterCell);
            Assert.AreEqual(surface.HexSize, blocked.HexSize);

            Assert.AreEqual(1, blocked.BlockedCells.Count);
            Assert.IsTrue(blocked.IsBlocked(blockedCell));
            Assert.IsTrue(blocked.Contains(blockedCell), "A blocked cell is still ground.");

            // The source surface is untouched (immutability).
            Assert.AreEqual(0, surface.BlockedCells.Count);
        }

        [Test]
        public void WithBlockedCells_FiltersCoordinatesOffTheSurface()
        {
            var surface = GenerateSurface();
            var blocked = surface.WithBlockedCells(new[] { new HexCoordinates(100, 100) });
            Assert.AreEqual(0, blocked.BlockedCells.Count);
        }

        [Test]
        public void SurfaceHexGrid_ExcludesBlockedCells()
        {
            var surface = GenerateSurface();
            var blockedCell = PickNonCenterCell(surface);
            var blocked = surface.WithBlockedCells(new[] { blockedCell });

            var grid = new SurfaceHexGrid();
            grid.Initialize(blocked, Center);

            Assert.AreEqual(surface.Cells.Count - 1, grid.GetCellsInBoundary().Count);
            Assert.IsFalse(grid.GetCellsInBoundary().Contains(blockedCell));
            Assert.IsFalse(grid.IsCellInBoundary(blockedCell));
            Assert.IsTrue(grid.IsCellInBoundary(blocked.CenterCell));
        }

        [Test]
        public void PlatformAnchor_NearestCellSkipsBlockedCells()
        {
            var surface = GenerateSurface();
            var blockedCell = PickNonCenterCell(surface);
            var (bx, bz) = surface.GetCellCenterLocal(blockedCell);
            var platformPosition = new Vector3(5f, 0f, 5f);
            // Aim exactly at the blocked cell's center: the anchor must land elsewhere.
            var target = platformPosition + new Vector3(bx, 0f, bz);

            var blocked = surface.WithBlockedCells(new[] { blockedCell });
            Vector3 landing = global::Platform.PlatformAnchor.NearestCellWorld(
                blocked, platformPosition, target);

            Assert.Greater((landing - target).sqrMagnitude, 1e-6f,
                "Landing must not target the blocked cell's center.");
        }
    }
}
