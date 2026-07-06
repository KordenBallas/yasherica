using System.Collections.Generic;
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
    public class EnvironmentDressingPlannerTests
    {
        private const int BattlefieldMin = 12;

        private class ThemeStub : Loot.Core.ICurrentThemeProvider
        {
            public LevelTheme CurrentTheme { get; private set; } = LevelTheme.Forest;
            public void SetTheme(LevelTheme theme) => CurrentTheme = theme;
        }

        private class SeedStub : Loot.Core.IRunSeedProvider
        {
            public int RunSeed { get; private set; } = 1234;
            public void SetSeed(int seed) => RunSeed = seed;
        }

        private static PlatformHexSurface GenerateSurface(ulong seed = 7)
        {
            return new PlatformSurfaceGenerator().Generate(
                new ShapeProfile(20, 30, 6), guaranteedMinCells: BattlefieldMin, hexSize: 2f,
                orientation: HexOrientation.Flat, rimWidth: 1.2f, rimJitterPercent: 35,
                rng: new DeterministicRandom(seed));
        }

        private static IBiomeFeaturePoolCatalog ForestCatalog()
        {
            var pool = new FeaturePoolData("forest-kit", new[]
            {
                new FeatureEntryData(FeatureKind.Blocking, 1, 1f, 1f),
                new FeatureEntryData(FeatureKind.SmallDecorative, 2, 1f, 1f)
            });
            return new BiomeFeaturePoolCatalog(
                new Dictionary<LevelTheme, (FeaturePoolData, FeatureDensitySettings)>
                {
                    [LevelTheme.Forest] = (pool, FeatureDensitySettings.CreateDefault())
                });
        }

        private static ISiteDressingCatalog CampCatalog()
        {
            return new SiteDressingCatalog(new Dictionary<string, SiteKitData>
            {
                ["camp-kit"] = new SiteKitData("camp-kit", 0, 4, 1, 0)
            });
        }

        private static EnvironmentDressingPlanner CreatePlanner(
            IBiomeFeaturePoolCatalog biomes = null, ISiteDressingCatalog sites = null)
        {
            return new EnvironmentDressingPlanner(
                biomes, sites, new ThemeStub(), new SeedStub());
        }

        [Test]
        public void WildNode_WithBoundKit_GetsABiomePlan()
        {
            var planner = CreatePlanner(ForestCatalog(), CampCatalog());
            var plan = planner.Plan(
                1, SiteStamp.Wild, PlatformContentKind.Combat, GenerateSurface(), BattlefieldMin);

            Assert.AreEqual(DressingPlanKind.BiomeFeatures, plan.Kind);
            Assert.AreEqual("forest-kit", plan.KitKey);
            Assert.IsFalse(plan.IsEmpty);
        }

        [Test]
        public void SiteNode_WithBoundKit_GetsASitePlan_NotBiomeFeatures()
        {
            var planner = CreatePlanner(ForestCatalog(), CampCatalog());
            var site = new SiteStamp("camp", 1, 0, 1, "camp-kit");
            var plan = planner.Plan(
                2, site, PlatformContentKind.Combat, GenerateSurface(), BattlefieldMin);

            Assert.AreEqual(DressingPlanKind.SiteDressing, plan.Kind);
            Assert.AreEqual("camp-kit", plan.KitKey);
        }

        [Test]
        public void MissingKits_FailSafeToTheEmptyPlan()
        {
            // No catalogs at all — every node gets the base layer, never a throw.
            var planner = CreatePlanner();
            var surface = GenerateSurface();

            Assert.IsTrue(planner.Plan(
                1, SiteStamp.Wild, PlatformContentKind.Combat, surface, BattlefieldMin).IsEmpty);
            Assert.IsTrue(planner.Plan(
                2, new SiteStamp("camp", 1, 0, 1, "unknown-kit"), PlatformContentKind.Combat,
                surface, BattlefieldMin).IsEmpty);
            Assert.IsTrue(planner.Plan(
                3, SiteStamp.Wild, PlatformContentKind.Combat, null, BattlefieldMin).IsEmpty);
        }

        [Test]
        public void SameNode_ReplansIdentically_IndependentOfCallOrder()
        {
            // Per-platform seed streams: node 5's plan is the same whether planned first or after
            // other nodes (order-independence across windows — the restore path relies on it).
            var surface = GenerateSurface();
            var plannerA = CreatePlanner(ForestCatalog(), CampCatalog());
            var plannerB = CreatePlanner(ForestCatalog(), CampCatalog());

            plannerB.Plan(1, SiteStamp.Wild, PlatformContentKind.Combat, GenerateSurface(3), BattlefieldMin);
            plannerB.Plan(2, SiteStamp.Wild, PlatformContentKind.Combat, GenerateSurface(4), BattlefieldMin);

            var direct = plannerA.Plan(5, SiteStamp.Wild, PlatformContentKind.Combat, surface, BattlefieldMin);
            var afterOthers = plannerB.Plan(5, SiteStamp.Wild, PlatformContentKind.Combat, surface, BattlefieldMin);

            Assert.AreEqual(direct.Placements.Count, afterOthers.Placements.Count);
            for (int i = 0; i < direct.Placements.Count; i++)
            {
                Assert.AreEqual(direct.Placements[i].LocalX, afterOthers.Placements[i].LocalX);
                Assert.AreEqual(direct.Placements[i].LocalZ, afterOthers.Placements[i].LocalZ);
            }
        }

        [Test]
        public void SitePlatforms_OfOneInstance_ShareTheInstanceDraws()
        {
            var siteCatalog = new SiteDressingCatalog(new Dictionary<string, SiteKitData>
            {
                ["settlement-kit"] = new SiteKitData("settlement-kit", 5, 3, 0, 2)
            });
            var planner = CreatePlanner(ForestCatalog(), siteCatalog);

            var scales = new List<float>();
            for (int index = 0; index < 3; index++)
            {
                var site = new SiteStamp("city", 7, index, 3, "settlement-kit");
                var plan = planner.Plan(
                    10 + index, site, PlatformContentKind.Combat,
                    GenerateSurface((ulong)(30 + index)), BattlefieldMin);
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
                Assert.AreEqual(scales[0], scale, 1e-5f,
                    "Block-shared structure scale must match across the instance's platforms.");
            }
        }
    }
}
