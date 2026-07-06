using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using LevelGeneration;
using LevelGeneration.Surface;
using Narrative.Director.Core;
using NUnit.Framework;
using World.Dressing.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeFeaturePlannerTests
    {
        private const int BattlefieldMin = 12;

        private static PlatformHexSurface GenerateSurface(ulong seed = 7, int min = 20, int max = 30)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(min, max, 6), guaranteedMinCells: BattlefieldMin, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static FeaturePoolData BuildPool()
        {
            return new FeaturePoolData("test-kit", new[]
            {
                new FeatureEntryData(FeatureKind.Blocking, 2, 0.9f, 1.3f),
                new FeatureEntryData(FeatureKind.Blocking, 3, 0.9f, 1.2f),
                new FeatureEntryData(FeatureKind.LargeDecorative, 3, 0.8f, 1.2f),
                new FeatureEntryData(FeatureKind.SmallDecorative, 4, 0.8f, 1.2f)
            });
        }

        private static PlatformDressingPlan Plan(
            PlatformHexSurface surface, ulong seed, FeaturePoolData pool = null,
            FeatureDensitySettings density = null, int battlefieldMin = BattlefieldMin)
        {
            return new BiomeFeaturePlanner().Plan(
                surface, pool ?? BuildPool(), density ?? FeatureDensitySettings.CreateDefault(),
                LevelTheme.Forest, battlefieldMin, new DeterministicRandom(seed));
        }

        [Test]
        public void SameSeed_ProducesIdenticalPlan()
        {
            var surface = GenerateSurface();
            var first = Plan(surface, 42);
            var second = Plan(surface, 42);

            CollectionAssert.AreEqual(first.BlockedCells.ToList(), second.BlockedCells.ToList());
            Assert.AreEqual(first.Placements.Count, second.Placements.Count);
            for (int i = 0; i < first.Placements.Count; i++)
            {
                Assert.AreEqual(first.Placements[i].EntryIndex, second.Placements[i].EntryIndex);
                Assert.AreEqual(first.Placements[i].LocalX, second.Placements[i].LocalX);
                Assert.AreEqual(first.Placements[i].LocalZ, second.Placements[i].LocalZ);
                Assert.AreEqual(first.Placements[i].YawDegrees, second.Placements[i].YawDegrees);
                Assert.AreEqual(first.Placements[i].Scale, second.Placements[i].Scale);
            }
        }

        [Test]
        public void Blockers_NeverLandOnProtectedCells()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var surface = GenerateSurface(seed);
                var density = FeatureDensitySettings.CreateDefault();
                var plan = Plan(surface, seed);
                var protectedSet = ProtectedCells.Build(
                    surface, density.LaneHalfWidth * surface.HexSize);

                foreach (var cell in plan.BlockedCells)
                {
                    Assert.IsFalse(protectedSet.Contains(cell),
                        $"Seed {seed}: blocker on protected cell ({cell.Q},{cell.R}).");
                }
            }
        }

        [Test]
        public void Blockers_HonourBattlefieldMinimum()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var surface = GenerateSurface(seed, 14, 18);
                // A hostile density that would flood the platform without the cap.
                var density = new FeatureDensitySettings(blockersPer100Cells: 80f);
                var plan = Plan(surface, seed, density: density);

                int free = surface.Cells.Count - plan.BlockedCells.Count;
                Assert.GreaterOrEqual(free, BattlefieldMin,
                    $"Seed {seed}: blocking pushed free cells under the battlefield minimum.");
            }
        }

        [Test]
        public void Blockers_KeepPairwiseSpacing()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var surface = GenerateSurface(seed);
                var blocked = Plan(surface, seed).BlockedCells.ToList();
                for (int i = 0; i < blocked.Count; i++)
                {
                    for (int j = i + 1; j < blocked.Count; j++)
                    {
                        Assert.GreaterOrEqual(
                            BlockedCellGuard.HexDistance(blocked[i], blocked[j]), 2,
                            $"Seed {seed}: blockers closer than the deliberate-obstacle spacing.");
                    }
                }
            }
        }

        [Test]
        public void Blockers_NeverDisconnectTheField()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var surface = GenerateSurface(seed);
                var blockedSet = new HashSet<HexCoordinates>(Plan(surface, seed).BlockedCells);
                Assert.IsTrue(BlockedCellGuard.StaysConnected(surface, blockedSet),
                    $"Seed {seed}: blocking split the free cells.");
            }
        }

        [Test]
        public void EmptyOrMissingPool_YieldsTheEmptyPlan()
        {
            var surface = GenerateSurface();
            Assert.IsTrue(Plan(surface, 42, new FeaturePoolData("empty", null)).IsEmpty);
            Assert.AreSame(PlatformDressingPlan.Empty, new BiomeFeaturePlanner().Plan(
                surface, null, FeatureDensitySettings.CreateDefault(), LevelTheme.Forest,
                BattlefieldMin, new DeterministicRandom(1)));
        }

        [Test]
        public void ZeroDensity_PlacesNothing_ButStillCarriesTheKit()
        {
            // No features at zero density — yet the plan keeps its kit identity, because the
            // biome ground material must dress featureless platforms too.
            var surface = GenerateSurface();
            var plan = Plan(surface, 42, density: new FeatureDensitySettings(0f, 0f));
            Assert.AreEqual(0, plan.BlockedCells.Count);
            Assert.AreEqual(0, plan.Placements.Count);
            Assert.AreEqual(DressingPlanKind.BiomeFeatures, plan.Kind);
            Assert.AreEqual("test-kit", plan.KitKey);
        }

        [Test]
        public void DecorativePlacements_OutnumberBlockersPerDensityDials()
        {
            // Default dials: 10 decorative clusters (2-5 members each) vs 4 blockers per 100 cells.
            var surface = GenerateSurface();
            var plan = Plan(surface, 42);

            int blockers = plan.BlockedCells.Count;
            int decoratives = plan.Placements.Count - blockers;
            Assert.Greater(decoratives, blockers,
                "Decor density is decoupled from (and exceeds) obstacle density.");
        }

        [Test]
        public void LargeDecorative_BiasesToTheRear()
        {
            // Over many seeds the mean Z of large-decorative placements sits behind the
            // surface's mean cell Z (+Z = away from the camera).
            var pool = new FeaturePoolData("large-only", new[]
            {
                new FeatureEntryData(FeatureKind.LargeDecorative, 1, 1f, 1f)
            });

            float placementZSum = 0f;
            int placementCount = 0;
            float surfaceZSum = 0f;
            int surfaceCount = 0;
            for (ulong seed = 1; seed <= 12; seed++)
            {
                var surface = GenerateSurface(seed);
                foreach (var cell in surface.Cells)
                {
                    surfaceZSum += surface.GetCellCenterLocal(cell).Z;
                    surfaceCount++;
                }

                var plan = Plan(surface, seed, pool);
                foreach (var placement in plan.Placements)
                {
                    placementZSum += placement.LocalZ;
                    placementCount++;
                }
            }

            Assert.Greater(placementCount, 0);
            Assert.Greater(placementZSum / placementCount, surfaceZSum / surfaceCount,
                "Large decoration should sit toward the rear on average.");
        }

        [Test]
        public void GroundedPlacements_FitFullyWithinTheSilhouette()
        {
            // No entry is overhang-flagged: every placement (blockers, cluster members, near-rim
            // jitter included) must clear the platform edge by its scaled footprint (edge-fit
            // brief FR1/FR3/FR4).
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var surface = GenerateSurface(seed);
                var pool = BuildPool();
                var plan = Plan(surface, seed, pool);
                foreach (var placement in plan.Placements)
                {
                    float required = pool.Entries[placement.EntryIndex].FootprintRadius * placement.Scale;
                    float clearance = PlatformEdgeFit.SignedClearance(
                        surface.Outline, placement.LocalX, placement.LocalZ);
                    Assert.GreaterOrEqual(clearance, required - 1e-3f,
                        $"Seed {seed}: grounded prop overhangs the edge (clearance {clearance:F2} < {required:F2}).");
                }
            }
        }

        [Test]
        public void OverhangFlaggedEntry_StillLeansPastTheEdge()
        {
            // The deliberate framing style survives as an opt-in: a flagged large prop anchors on
            // the rim (beyond the walkable outline) at least somewhere across seeds (brief FR5).
            var pool = new FeaturePoolData("framing", new[]
            {
                new FeatureEntryData(FeatureKind.LargeDecorative, 1, 1f, 1f, mayOverhang: true)
            });

            bool leanedOut = false;
            for (ulong seed = 1; seed <= 12 && !leanedOut; seed++)
            {
                var surface = GenerateSurface(seed);
                var plan = Plan(surface, seed, pool);
                foreach (var placement in plan.Placements)
                {
                    if (PlatformEdgeFit.SignedClearance(
                            surface.Outline, placement.LocalX, placement.LocalZ) < 0f)
                    {
                        leanedOut = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(leanedOut, "A may-overhang prop never anchored beyond the walkable edge.");
        }

        [Test]
        public void PlanCarriesKitKeyAndKind()
        {
            var surface = GenerateSurface();
            var plan = Plan(surface, 42);
            Assert.AreEqual(DressingPlanKind.BiomeFeatures, plan.Kind);
            Assert.AreEqual("test-kit", plan.KitKey);
            Assert.AreEqual(LevelTheme.Forest, plan.Theme);
        }
    }
}
