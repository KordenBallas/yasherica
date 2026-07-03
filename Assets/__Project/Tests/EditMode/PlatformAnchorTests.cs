using Combat.Battlefield;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using Platform;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformAnchorTests
    {
        private static readonly Vector3 Position = new Vector3(25f, 3f, -4f);

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(12, 18, 6), guaranteedMinCells: 12, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        [Test]
        public void CenterCellWorld_IsTheCenterCellOffsetFromThePlatform()
        {
            var surface = GenerateSurface();

            var (cx, cz) = surface.GetCellCenterLocal(surface.CenterCell);
            Vector3 world = PlatformAnchor.CenterCellWorld(surface, Position);

            Assert.AreEqual(Position.x + cx, world.x, 1e-4f);
            Assert.AreEqual(Position.y, world.y, 1e-4f);
            Assert.AreEqual(Position.z + cz, world.z, 1e-4f);
        }

        [Test]
        public void NearestCellWorld_MinimizesXZDistance_OverAllCells()
        {
            var surface = GenerateSurface();
            var approach = new Vector3(60f, 0f, 12f); // Far off to one side, like a jumping hero.

            Vector3 landing = PlatformAnchor.NearestCellWorld(surface, Position, approach);

            float landingDistSq = (landing.x - approach.x) * (landing.x - approach.x)
                                + (landing.z - approach.z) * (landing.z - approach.z);
            bool isACellCenter = false;
            foreach (var cell in surface.Cells)
            {
                var (x, z) = surface.GetCellCenterLocal(cell);
                float dx = Position.x + x - approach.x;
                float dz = Position.z + z - approach.z;
                Assert.LessOrEqual(landingDistSq, dx * dx + dz * dz + 1e-4f);
                if (Mathf.Abs(Position.x + x - landing.x) < 1e-4f && Mathf.Abs(Position.z + z - landing.z) < 1e-4f)
                {
                    isACellCenter = true;
                }
            }

            Assert.IsTrue(isACellCenter, "Landing must be exactly a walkable cell center");
            Assert.AreEqual(Position.y, landing.y, 1e-4f);
        }

        [Test]
        public void MissingSurface_FallsBackToThePlatformOrigin()
        {
            Assert.AreEqual(Position, PlatformAnchor.CenterCellWorld(null, Position));
            Assert.AreEqual(Position, PlatformAnchor.NearestCellWorld(null, Position, Vector3.zero));
        }
    }
}
