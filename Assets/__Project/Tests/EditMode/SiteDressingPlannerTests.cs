using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using LevelGeneration;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using World.Dressing.Core;
using World.Sites.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class SiteDressingPlannerTests
    {
        private const int BattlefieldMin = 12;
        private const int InstanceSeed = 900;

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(20, 30, 6), guaranteedMinCells: BattlefieldMin, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static SiteKitData SettlementKit()
            => new SiteKitData("settlement-kit", structureCount: 5, propCount: 5, focalCount: 0, gateCount: 4);

        private static SiteKitData CampKit()
            => new SiteKitData("camp-kit", structureCount: 0, propCount: 9, focalCount: 3, gateCount: 0);

        private static PlatformDressingPlan Plan(
            PlatformHexSurface surface, SiteKitData kit, int index, int footprint = 3,
            ulong platformSeed = 5, int battlefieldMin = BattlefieldMin)
        {
            var site = new SiteStamp(kit.DressingThemeId.Split('-')[0], 1, index, footprint,
                kit.DressingThemeId);
            // The orchestrator derives a fresh instance stream per platform — same draws each time.
            return new SiteDressingPlanner().Plan(
                surface, site, kit, LevelTheme.Forest, battlefieldMin,
                new DeterministicRandom(InstanceSeed), new DeterministicRandom(platformSeed));
        }

        [Test]
        public void SameSeeds_ReplayTheIdenticalPlan()
        {
            var surface = GenerateSurface();
            var first = Plan(surface, SettlementKit(), index: 0);
            var second = Plan(surface, SettlementKit(), index: 0);

            CollectionAssert.AreEqual(first.BlockedCells.ToList(), second.BlockedCells.ToList());
            Assert.AreEqual(first.Placements.Count, second.Placements.Count);
            for (int i = 0; i < first.Placements.Count; i++)
            {
                Assert.AreEqual(first.Placements[i].Role, second.Placements[i].Role);
                Assert.AreEqual(first.Placements[i].LocalX, second.Placements[i].LocalX);
                Assert.AreEqual(first.Placements[i].LocalZ, second.Placements[i].LocalZ);
            }
        }

        [Test]
        public void Structures_ShareScaleAcrossTheBlock()
        {
            // Different platforms (index, surface, platform stream) — the block-shared draws
            // (skyline band, structure scale) come from the instance stream and must match.
            var scales = new List<float>();
            for (int index = 0; index < 3; index++)
            {
                var surface = GenerateSurface((ulong)(20 + index));
                var plan = Plan(surface, SettlementKit(), index, platformSeed: (ulong)(50 + index));
                foreach (var placement in plan.Placements)
                {
                    if (placement.Role == DressingRole.Structure)
                    {
                        scales.Add(placement.Scale);
                    }
                }
            }

            Assert.Greater(scales.Count, 1);
            foreach (var scale in scales)
            {
                Assert.AreEqual(scales[0], scale, 1e-5f, "Structure scale is block-shared.");
            }
        }

        [Test]
        public void Structures_SitInTheRearAndFaceTheCamera()
        {
            var surface = GenerateSurface();
            var plan = Plan(surface, SettlementKit(), index: 1);

            var structures = plan.Placements.Where(p => p.Role == DressingRole.Structure).ToList();
            Assert.Greater(structures.Count, 0);

            float meanZ = 0f;
            int count = 0;
            foreach (var cell in surface.Cells)
            {
                meanZ += surface.GetCellCenterLocal(cell).Z;
                count++;
            }

            meanZ /= count;
            foreach (var structure in structures)
            {
                Assert.Greater(structure.LocalZ, meanZ, "Structures group to the rear.");
                Assert.AreEqual(180f, structure.YawDegrees, 1e-3f, "House fronts face the camera.");
            }
        }

        [Test]
        public void StructureCells_AreBlockedAndKeepTheFieldLegal()
        {
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var surface = GenerateSurface(seed);
                var plan = Plan(surface, SettlementKit(), index: 0, platformSeed: seed);

                int structures = plan.Placements.Count(p => p.Role == DressingRole.Structure);
                Assert.AreEqual(structures, plan.BlockedCells.Count,
                    "Every structure consumes exactly one cell.");
                Assert.GreaterOrEqual(surface.Cells.Count - plan.BlockedCells.Count, BattlefieldMin);
                Assert.IsTrue(BlockedCellGuard.StaysConnected(
                    surface, new HashSet<HexCoordinates>(plan.BlockedCells)));
            }
        }

        [Test]
        public void Gate_AppearsOnlyOnTheAnchorPlatform()
        {
            var surface = GenerateSurface();

            var anchor = Plan(surface, SettlementKit(), index: 0);
            Assert.IsTrue(anchor.Placements.Any(p => p.Role == DressingRole.Gate),
                "The anchor platform carries the threshold.");

            var inner = Plan(surface, SettlementKit(), index: 1);
            Assert.IsFalse(inner.Placements.Any(p => p.Role == DressingRole.Gate));
        }

        [Test]
        public void Gate_SitsOnTheApproachSide()
        {
            // The gate marks arrival where the movement lane enters the platform: west of the
            // surface's mean X (the exact edge cell depends on the lane band, not the global rim).
            var surface = GenerateSurface();
            var plan = Plan(surface, SettlementKit(), index: 0);
            float meanX = surface.Cells.Average(c => surface.GetCellCenterLocal(c).X);

            var gates = plan.Placements.Where(p => p.Role == DressingRole.Gate).ToList();
            Assert.Greater(gates.Count, 0);
            foreach (var gate in gates)
            {
                Assert.Less(gate.LocalX, meanX, "Gate pieces sit on the approach (min-X) side.");
            }
        }

        [Test]
        public void Camp_StacksTheFocalOnOneBlockedCellWithPropsAround()
        {
            var surface = GenerateSurface();
            var plan = Plan(surface, CampKit(), index: 0);

            Assert.AreEqual(1, plan.BlockedCells.Count, "The fire consumes exactly one cell.");
            var focal = plan.Placements.Where(p => p.Role == DressingRole.Focal).ToList();
            Assert.AreEqual(3, focal.Count, "Every focal entry stacks on the fire cell.");
            float fx = focal[0].LocalX;
            float fz = focal[0].LocalZ;
            foreach (var piece in focal)
            {
                Assert.AreEqual(fx, piece.LocalX, 1e-4f);
                Assert.AreEqual(fz, piece.LocalZ, 1e-4f);
            }

            var props = plan.Placements.Where(p => p.Role == DressingRole.Prop).ToList();
            Assert.GreaterOrEqual(props.Count, 3);
            foreach (var prop in props)
            {
                float dx = prop.LocalX - fx;
                float dz = prop.LocalZ - fz;
                float distance = (float)System.Math.Sqrt(dx * dx + dz * dz);
                // Members sit in a ring around the fire; one pulled back from the platform edge
                // may end up closer than the rolled radius — gathered, never past the silhouette.
                Assert.LessOrEqual(distance, surface.HexSize * 1.7f,
                    "Props gather around the fire.");
            }
        }

        [Test]
        public void UnplaceablePlatform_StillCarriesTheKit_ForTheGroundOverlay()
        {
            // Battlefield minimum eats the whole platform: no structure fits, no gate (inner
            // platform) — yet the plan keeps its kit identity so the shared site ground applies.
            var surface = GenerateSurface();
            var plan = Plan(surface, SettlementKit(), index: 1, battlefieldMin: surface.Cells.Count);

            Assert.AreEqual(0, plan.BlockedCells.Count);
            Assert.AreEqual(DressingPlanKind.SiteDressing, plan.Kind);
            Assert.AreEqual("settlement-kit", plan.KitKey);
        }

        [Test]
        public void WildOrEmptyKit_YieldsTheEmptyPlan()
        {
            var surface = GenerateSurface();
            var planner = new SiteDressingPlanner();

            var wildPlan = planner.Plan(
                surface, SiteStamp.Wild, SettlementKit(), LevelTheme.Forest, BattlefieldMin,
                new DeterministicRandom(1), new DeterministicRandom(2));
            Assert.IsTrue(wildPlan.IsEmpty);

            var emptyKitPlan = planner.Plan(
                surface, new SiteStamp("camp", 1, 0, 1, "camp-kit"),
                new SiteKitData("camp-kit", 0, 0, 0, 0), LevelTheme.Forest, BattlefieldMin,
                new DeterministicRandom(1), new DeterministicRandom(2));
            Assert.IsTrue(emptyKitPlan.IsEmpty);
        }

        [Test]
        public void SitePlacements_FitWithinTheSilhouette()
        {
            // The playtest fix: no site dressing (house, prop ring, gate) hangs past the walkable
            // outline — site kits have no overhang opt-in, everything sits on the platform.
            foreach (var kit in new[] { SettlementKit(), CampKit() })
            {
                for (ulong seed = 1; seed <= 6; seed++)
                {
                    var surface = GenerateSurface(seed);
                    var plan = Plan(surface, kit, index: 0, platformSeed: seed);
                    foreach (var placement in plan.Placements)
                    {
                        float clearance = PlatformEdgeFit.SignedClearance(
                            surface.Outline, placement.LocalX, placement.LocalZ);
                        Assert.GreaterOrEqual(clearance, 0f,
                            $"Seed {seed}: {placement.Role} placement past the platform silhouette.");
                    }
                }
            }
        }

        [Test]
        public void AllPlacements_StayWithinTheSurfaceBounds()
        {
            // Platform-local placements can never cross a gap; assert they stay inside the
            // outline+rim extents.
            foreach (var kit in new[] { SettlementKit(), CampKit() })
            {
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    var surface = GenerateSurface(seed);
                    float minX = surface.RimRing.Min(p => p.X) - surface.HexSize;
                    float maxX = surface.RimRing.Max(p => p.X) + surface.HexSize;
                    float minZ = surface.RimRing.Min(p => p.Z) - surface.HexSize;
                    float maxZ = surface.RimRing.Max(p => p.Z) + surface.HexSize;

                    var plan = Plan(surface, kit, index: 0, platformSeed: seed);
                    foreach (var placement in plan.Placements)
                    {
                        Assert.IsTrue(
                            placement.LocalX >= minX && placement.LocalX <= maxX
                            && placement.LocalZ >= minZ && placement.LocalZ <= maxZ,
                            $"Seed {seed}: {placement.Role} placement outside the platform bounds.");
                    }
                }
            }
        }
    }
}
