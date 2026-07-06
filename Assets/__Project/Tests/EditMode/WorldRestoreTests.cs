using System.Collections.Generic;
using Core.Logging;
using Core.Persistence;
using LevelGeneration;
using Loot.Core;
using Narrative.Director.Core;
using NUnit.Framework;
using World.Sites.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// P2-2 world-resume determinism at the allocator level (FR12): the run-scoped cursors that
    /// ride OUTSIDE the shared PRNG state (quest spacing, pending site blocks, site instance ids)
    /// round-trip through the save snapshot, so a restored allocation stream continues exactly
    /// where the interrupted one left off.
    /// </summary>
    [TestFixture]
    public class WorldRestoreTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeThemeProvider : ICurrentThemeProvider
        {
            public LevelTheme CurrentTheme => LevelTheme.Forest;
            public void SetTheme(LevelTheme theme) { }
        }

        private static WorldContentDensitySettings Density() =>
            new WorldContentDensitySettings(
                averagePlatformsPerQuest: 4, minPlatformsBetweenQuests: 2,
                emptyWeight: 2, lootWeight: 1, combatWeight: 1,
                averagePlatformsPerAmbientSite: 3, minPlatformsBetweenSites: 1, wildQuestWeight: 1);

        private static IBiomeMonsterPoolCatalog Pool() =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, new[] { 5, 9 } }
            });

        private static SiteDefinitionData AmbientSite() =>
            new SiteDefinitionData("lair", "landmark", 3, 3, 1,
                new[] { new ContentBeat(ContentBaseKind.Combat, "den-monster") },
                1, 1,
                new[] { new WeightedBeat(new ContentBeat(ContentBaseKind.Loot, "scattered"), 1) },
                "lair-kit");

        private static (WorldContentAllocator inner, SiteAwareSlotAllocator outer, DeterministicRandom rng)
            BuildAllocators(ulong seed)
        {
            var rng = new DeterministicRandom(seed);
            var density = Density();
            var pools = Pool();
            var themes = new FakeThemeProvider();
            var inner = new WorldContentAllocator(density, pools, themes, rng, new FakeLogger());
            var outer = new SiteAwareSlotAllocator(inner, new SiteCatalog(new[] { AmbientSite() }),
                new SiteBlockBuilder(), density, pools, themes, rng, new FakeLogger());
            return (inner, outer, rng);
        }

        [Test]
        public void AllocatorCursors_RoundTrip_ContinuesTheStreamIdentically()
        {
            // The interrupted run: draw some slots, then "save" mid-stream (possibly mid site block).
            var a = BuildAllocators(seed: 11);
            for (int i = 0; i < 17; i++)
            {
                a.outer.AllocateSlot(questAvailable: i % 3 == 0, currentTier: 1);
            }

            ulong savedRngState = a.rng.State;
            var savedPending = a.outer.PendingSlots;
            int savedSiteSpacing = a.outer.PlatformsSinceSite;
            int savedNextInstance = a.outer.NextInstanceId;
            int savedQuestSpacing = a.inner.PlatformsSinceQuest;

            // The continued run: fresh instances (a new scene container), state restored.
            var b = BuildAllocators(seed: 999); // wrong seed on purpose — the restored state must win
            b.rng.State = savedRngState;
            b.inner.RestoreCursor(savedQuestSpacing);
            b.outer.RestoreState(savedPending, savedSiteSpacing, savedNextInstance);

            for (int i = 0; i < 40; i++)
            {
                bool quest = i % 3 == 1;
                var expected = a.outer.AllocateSlot(quest, currentTier: 1);
                var actual = b.outer.AllocateSlot(quest, currentTier: 1);
                Assert.AreEqual(expected.Kind, actual.Kind, $"Kind diverged at slot {i} (FR12).");
                Assert.AreEqual(expected.EnemyId, actual.EnemyId, $"EnemyId diverged at slot {i}.");
                Assert.AreEqual(expected.Site.SiteId, actual.Site.SiteId, $"Site diverged at slot {i}.");
                Assert.AreEqual(expected.Site.InstanceId, actual.Site.InstanceId,
                    $"Site instance diverged at slot {i}.");
                Assert.AreEqual(expected.Site.Index, actual.Site.Index, $"Site index diverged at slot {i}.");
            }
        }

        private static SiteDefinitionData CampSite() =>
            new SiteDefinitionData("camp", "settlement", 1, 1, 1,
                new[] { new ContentBeat(ContentBaseKind.Combat, "bandit") },
                0, 0,
                System.Array.Empty<WeightedBeat>(),
                "camp-kit",
                bossStoryFlavor: "bandit-boss", bossCrewMin: 2, bossCrewMax: 4);

        private static (WorldContentAllocator inner, SiteAwareSlotAllocator outer, DeterministicRandom rng)
            BuildCampAllocators(ulong seed)
        {
            var rng = new DeterministicRandom(seed);
            var density = Density();
            var pools = Pool();
            var themes = new FakeThemeProvider();
            var inner = new WorldContentAllocator(density, pools, themes, rng, new FakeLogger());
            var outer = new SiteAwareSlotAllocator(inner, new SiteCatalog(new[] { CampSite() }),
                new SiteBlockBuilder(), density, pools, themes, rng, new FakeLogger());
            return (inner, outer, rng);
        }

        [Test]
        public void CampAllocation_RoundTrip_ReproducesKindAndCrew()
        {
            // Same shape as the cursor round-trip above, with a boss-led camp in the catalog: the
            // restored stream must reproduce Camp allocations bit-exactly, crew ids included.
            var a = BuildCampAllocators(seed: 23);
            for (int i = 0; i < 13; i++)
            {
                a.outer.AllocateSlot(questAvailable: false, currentTier: 1);
            }

            var b = BuildCampAllocators(seed: 777);
            b.rng.State = a.rng.State;
            b.inner.RestoreCursor(a.inner.PlatformsSinceQuest);
            b.outer.RestoreState(a.outer.PendingSlots, a.outer.PlatformsSinceSite, a.outer.NextInstanceId);

            bool sawCamp = false;
            for (int i = 0; i < 60; i++)
            {
                var expected = a.outer.AllocateSlot(questAvailable: false, currentTier: 1);
                var actual = b.outer.AllocateSlot(questAvailable: false, currentTier: 1);
                Assert.AreEqual(expected.Kind, actual.Kind, $"Kind diverged at slot {i}.");
                CollectionAssert.AreEqual(expected.CrewEnemyIds, actual.CrewEnemyIds,
                    $"Crew diverged at slot {i}.");
                if (expected.Kind == WorldSlotKind.Camp)
                {
                    sawCamp = true;
                    Assert.GreaterOrEqual(expected.CrewEnemyIds.Count, 2);
                    Assert.LessOrEqual(expected.CrewEnemyIds.Count, 4);
                }
            }

            Assert.IsTrue(sawCamp, "Expected at least one Camp allocation in 60 slots.");
        }

        [Test]
        public void BridgeRestoreCursors_RehydratesPendingSiteBlocks()
        {
            var target = BuildAllocators(seed: 5);
            var bridge = new WorldStatePersistenceBridge(target.inner, target.outer);

            var world = new WorldStateSnapshot
            {
                PlatformsSinceQuest = 7,
                SiteAllocator = new SiteAllocatorSnapshot
                {
                    PlatformsSinceSite = 0,
                    NextSiteInstanceId = 3,
                    PendingSlots =
                    {
                        new PendingSiteSlotDto
                        {
                            BeatKind = (int)ContentBaseKind.Loot,
                            Flavor = "stash",
                            SiteId = "lair",
                            SiteInstanceId = 2,
                            SiteIndex = 1,
                            SiteFootprint = 2,
                            SiteDressingThemeId = "lair-kit"
                        }
                    }
                }
            };

            bridge.RestoreCursors(world);

            Assert.AreEqual(7, target.inner.PlatformsSinceQuest);
            Assert.AreEqual(3, target.outer.NextInstanceId);
            Assert.AreEqual(1, target.outer.PendingSlots.Count);

            // The rehydrated queue drains first, with the exact stamp it was saved with.
            var slot = target.outer.AllocateSlot(questAvailable: false, currentTier: 1);
            Assert.AreEqual(WorldSlotKind.Loot, slot.Kind);
            Assert.AreEqual("stash", slot.Flavor);
            Assert.AreEqual("lair", slot.Site.SiteId);
            Assert.AreEqual(2, slot.Site.InstanceId);
            Assert.AreEqual(1, slot.Site.Index);
            Assert.AreEqual(2, slot.Site.Footprint);
            Assert.AreEqual("lair-kit", slot.Site.DressingThemeId);
            Assert.AreEqual(0, target.outer.PendingSlots.Count);
        }

        [Test]
        public void CaptureWithoutACoordinator_YieldsNull_NotACrash()
        {
            var target = BuildAllocators(seed: 5);
            var bridge = new WorldStatePersistenceBridge(target.inner, target.outer);
            Assert.IsNull(bridge.Capture());
            bridge.RestoreCursors(null); // null world section is a no-op
        }
    }
}
