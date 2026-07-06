using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using Loot.Core;
using Narrative.Director.Core;
using NUnit.Framework;
using World.Sites.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class SiteAwareSlotAllocatorTests
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

        private static WorldContentDensitySettings Density(
            int avgPerQuest = 100, int minQuestSpacing = 0,
            int empty = 1, int loot = 0, int combat = 0,
            int avgPerAmbientSite = 0, int minSiteSpacing = 0, int wildQuestWeight = 40) =>
            new WorldContentDensitySettings(avgPerQuest, minQuestSpacing, empty, loot, combat,
                avgPerAmbientSite, minSiteSpacing, wildQuestWeight);

        private static IBiomeMonsterPoolCatalog Pool(params int[] enemyIds) =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<int>>
            {
                { LevelTheme.Forest, enemyIds }
            });

        private static SiteDefinitionData QuestSite(string id = "city", int footprint = 4, int weight = 1) =>
            new SiteDefinitionData(id, "settlement", footprint, footprint, weight,
                new[] { new ContentBeat(ContentBaseKind.Npc, "quest-bearer") },
                1, 1,
                new[] { new WeightedBeat(new ContentBeat(ContentBaseKind.Npc, "townsfolk"), 1) },
                "city-kit");

        private static SiteDefinitionData AmbientSite(string id = "lair", int footprint = 2, int weight = 1) =>
            new SiteDefinitionData(id, "landmark", footprint, footprint, weight,
                new[] { new ContentBeat(ContentBaseKind.Combat, "den-monster") },
                1, 1,
                new[] { new WeightedBeat(new ContentBeat(ContentBaseKind.Loot, "scattered"), 1) },
                "lair-kit");

        private static SiteDefinitionData CampSite(string id = "camp", int crewMin = 2, int crewMax = 4) =>
            new SiteDefinitionData(id, "settlement", 1, 1, 1,
                new[] { new ContentBeat(ContentBaseKind.Combat, "bandit") },
                0, 0,
                System.Array.Empty<WeightedBeat>(),
                "camp-kit",
                bossStoryFlavor: "bandit-boss", bossCrewMin: crewMin, bossCrewMax: crewMax);

        private static SiteAwareSlotAllocator Allocator(
            WorldContentDensitySettings density,
            ISiteCatalog catalog,
            IBiomeMonsterPoolCatalog pools = null,
            ulong seed = 7)
        {
            pools = pools ?? new BiomeMonsterPoolCatalog((Dictionary<LevelTheme, IReadOnlyList<int>>)null);
            var random = new DeterministicRandom(seed);
            var themes = new FakeThemeProvider();
            var inner = new WorldContentAllocator(density, pools, themes, random, new FakeLogger());
            return new SiteAwareSlotAllocator(inner, catalog, new SiteBlockBuilder(), density,
                pools, themes, random, new FakeLogger());
        }

        [Test]
        public void EmptyCatalog_IsABitExactPassthrough()
        {
            var density = Density(avgPerQuest: 3, minQuestSpacing: 1, empty: 2, loot: 1, combat: 1,
                avgPerAmbientSite: 5, minSiteSpacing: 0);

            var raw = new WorldContentAllocator(density, Pool(5, 9), new FakeThemeProvider(),
                new DeterministicRandom(42), new FakeLogger());
            var wrapped = Allocator(density, new SiteCatalog(null), Pool(5, 9), seed: 42);

            for (int i = 0; i < 60; i++)
            {
                var expected = raw.AllocateSlot(questAvailable: true, currentTier: 1);
                var actual = wrapped.AllocateSlot(questAvailable: true, currentTier: 1);
                Assert.AreEqual(expected.Kind, actual.Kind, $"Kind diverged at slot {i}.");
                Assert.AreEqual(expected.EnemyId, actual.EnemyId, $"EnemyId diverged at slot {i}.");
                Assert.IsTrue(actual.Site.IsWild, $"Slot {i} carries a site with no catalog.");

                if (actual.Kind == WorldSlotKind.Quest)
                {
                    // The planner would reserve here; with no quest sites it must not consume a roll.
                    Assert.IsTrue(wrapped.TryReserveSettlement(null).IsWild);
                }
            }
        }

        [Test]
        public void AmbientGate_HonorsSpacing_MeasuredFromBlockEnd()
        {
            const int spacing = 3;
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 2) });
            var allocator = Allocator(
                Density(avgPerAmbientSite: 1, minSiteSpacing: spacing),
                catalog, Pool(5));

            int lastBlockEnd = -1;
            int pendingInBlock = 0;
            for (int i = 0; i < 60; i++)
            {
                var slot = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
                if (slot.Site.IsWild)
                {
                    Assert.AreEqual(0, pendingInBlock, $"Wild slot {i} interrupted a site block.");
                    continue;
                }

                if (slot.Site.Index == 0)
                {
                    Assert.AreEqual(0, pendingInBlock, $"Anchor at {i} arrived before the previous block drained.");
                    if (lastBlockEnd >= 0)
                    {
                        Assert.Greater(i - lastBlockEnd, spacing,
                            $"Sites at block-end {lastBlockEnd} and anchor {i} violate spacing {spacing}.");
                    }

                    pendingInBlock = slot.Site.Footprint - 1;
                }
                else
                {
                    pendingInBlock--;
                    if (pendingInBlock == 0)
                    {
                        lastBlockEnd = i;
                    }
                }
            }

            Assert.GreaterOrEqual(lastBlockEnd, 0, "Expected at least one full site block in 60 slots.");
        }

        [Test]
        public void AmbientDisabled_NeverTriggersSites()
        {
            var catalog = new SiteCatalog(new[] { AmbientSite() });
            var allocator = Allocator(Density(avgPerAmbientSite: 0), catalog, Pool(5));

            for (int i = 0; i < 30; i++)
            {
                Assert.IsTrue(allocator.AllocateSlot(questAvailable: false, currentTier: 1).Site.IsWild);
            }
        }

        [Test]
        public void BlockQueue_DrainsInOrder_WithSequentialStamps()
        {
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 4) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, Pool(5));

            // Find the anchor, then the next three slots must be its block, indices 1..3.
            SlotAllocation anchor = default;
            for (int i = 0; i < 30 && anchor.Site.IsWild; i++)
            {
                anchor = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
            }

            Assert.IsFalse(anchor.Site.IsWild, "Expected an ambient site to trigger.");
            Assert.AreEqual(0, anchor.Site.Index);
            Assert.AreEqual(WorldSlotKind.Combat, anchor.Kind, "The lair anchor is its monster.");
            Assert.AreEqual("den-monster", anchor.Flavor);

            for (int index = 1; index < 4; index++)
            {
                var slot = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
                Assert.AreEqual("lair", slot.Site.SiteId);
                Assert.AreEqual(anchor.Site.InstanceId, slot.Site.InstanceId);
                Assert.AreEqual(index, slot.Site.Index);
            }
        }

        [Test]
        public void QuestSlot_TakesPrecedenceOverTheAmbientGate()
        {
            var catalog = new SiteCatalog(new[] { AmbientSite() });
            var allocator = Allocator(
                Density(avgPerQuest: 1, minQuestSpacing: 0, avgPerAmbientSite: 1),
                catalog, Pool(5));

            var slot = allocator.AllocateSlot(questAvailable: true, currentTier: 1);
            Assert.AreEqual(WorldSlotKind.Quest, slot.Kind);
            Assert.IsTrue(slot.Site.IsWild, "The quest slot itself is unstamped until the planner reserves.");
        }

        [Test]
        public void TryReserveSettlement_HonorsTheSiteTag()
        {
            var catalog = new SiteCatalog(new[] { QuestSite("city", footprint: 4), QuestSite("village", footprint: 2) });
            var allocator = Allocator(Density(), catalog, Pool(5));

            var stamp = allocator.TryReserveSettlement(new[] { "bounty", "site:village" });

            Assert.AreEqual("village", stamp.SiteId);
            Assert.AreEqual(0, stamp.Index);

            // The rest of the village block drains next.
            var next = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
            Assert.AreEqual("village", next.Site.SiteId);
            Assert.AreEqual(1, next.Site.Index);
        }

        [Test]
        public void TryReserveSettlement_UnknownTagAndZeroSiteWeights_StaysWild()
        {
            var catalog = new SiteCatalog(new[] { QuestSite("city", weight: 0) });
            var allocator = Allocator(Density(wildQuestWeight: 1), catalog, Pool(5));

            Assert.IsTrue(allocator.TryReserveSettlement(new[] { "site:atlantis" }).IsWild);
        }

        [Test]
        public void TryReserveSettlement_ZeroWildWeight_AlwaysPullsASettlement()
        {
            var catalog = new SiteCatalog(new[] { QuestSite("city", footprint: 4, weight: 1) });
            var allocator = Allocator(Density(wildQuestWeight: 0), catalog, Pool(5));

            var stamp = allocator.TryReserveSettlement(null);

            Assert.AreEqual("city", stamp.SiteId);
            Assert.AreEqual(4, stamp.Footprint);
        }

        [Test]
        public void SiteCombatSlot_WithEmptyPool_DowngradesToEmpty_KeepingTheStamp()
        {
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 1) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, pools: null);

            SlotAllocation anchor = default;
            for (int i = 0; i < 30 && anchor.Site.IsWild; i++)
            {
                anchor = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
            }

            Assert.IsFalse(anchor.Site.IsWild, "Expected the lair to trigger.");
            Assert.AreEqual(WorldSlotKind.Empty, anchor.Kind, "No pool: the site fight downgrades honestly.");
        }

        [Test]
        public void SiteCombatSlot_PicksAFlavorTaggedEnemy_WhenOneExists()
        {
            // Pool: enemy 5 untagged (the wild beast), enemy 9 tagged den-monster. The lair anchor
            // (Combat·den-monster) must always draw the tagged one.
            var pools = new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>
            {
                {
                    LevelTheme.Forest, new[]
                    {
                        new MonsterPoolEntry(5, new[] { "wild-beast" }),
                        new MonsterPoolEntry(9, new[] { "den-monster" })
                    }
                }
            });
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 1) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, pools);

            for (int i = 0; i < 20; i++)
            {
                var slot = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
                if (!slot.Site.IsWild)
                {
                    Assert.AreEqual(WorldSlotKind.Combat, slot.Kind);
                    Assert.AreEqual(9, slot.EnemyId, "The den-monster beat must draw the tagged enemy.");
                }
            }
        }

        [Test]
        public void SiteCombatSlot_UnmatchedFlavor_FallsBackToTheUnfilteredPool()
        {
            // No enemy carries 'den-monster': the fight still lands from the whole pool (warned once).
            var pools = new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>
            {
                { LevelTheme.Forest, new[] { new MonsterPoolEntry(5, new[] { "wild-beast" }) } }
            });
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 1) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, pools);

            SlotAllocation anchor = default;
            for (int i = 0; i < 30 && anchor.Site.IsWild; i++)
            {
                anchor = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
            }

            Assert.IsFalse(anchor.Site.IsWild);
            Assert.AreEqual(WorldSlotKind.Combat, anchor.Kind);
            Assert.AreEqual(5, anchor.EnemyId);
        }

        [Test]
        public void SameSeed_ProducesIdenticalSiteSequence()
        {
            var density = Density(avgPerQuest: 4, minQuestSpacing: 1, empty: 2, loot: 1, combat: 1,
                avgPerAmbientSite: 6, minSiteSpacing: 2);
            var sites = new[] { AmbientSite("lair"), AmbientSite("ruin"), QuestSite("city"), QuestSite("village") };

            var a = Allocator(density, new SiteCatalog(sites), Pool(5, 9), seed: 1234);
            var b = Allocator(density, new SiteCatalog(sites), Pool(5, 9), seed: 1234);

            for (int i = 0; i < 80; i++)
            {
                var slotA = a.AllocateSlot(questAvailable: true, currentTier: 1);
                var slotB = b.AllocateSlot(questAvailable: true, currentTier: 1);
                Assert.AreEqual(slotA.Kind, slotB.Kind, $"Kind diverged at slot {i}.");
                Assert.AreEqual(slotA.EnemyId, slotB.EnemyId, $"EnemyId diverged at slot {i}.");
                Assert.AreEqual(slotA.Flavor, slotB.Flavor, $"Flavor diverged at slot {i}.");
                Assert.AreEqual(slotA.Site.SiteId, slotB.Site.SiteId, $"SiteId diverged at slot {i}.");
                Assert.AreEqual(slotA.Site.Index, slotB.Site.Index, $"Site index diverged at slot {i}.");

                if (slotA.Kind == WorldSlotKind.Quest)
                {
                    var stampA = a.TryReserveSettlement(null);
                    var stampB = b.TryReserveSettlement(null);
                    Assert.AreEqual(stampA.SiteId, stampB.SiteId, $"Settlement diverged at slot {i}.");
                }
            }
        }

        // --- Boss-led camp anchor (bandit-camp brief) ---

        private SlotAllocation FirstAnchor(SiteAwareSlotAllocator allocator)
        {
            SlotAllocation anchor = default;
            for (int i = 0; i < 40 && anchor.Site.IsWild; i++)
            {
                anchor = allocator.AllocateSlot(questAvailable: false, currentTier: 1);
            }

            Assert.IsFalse(anchor.Site.IsWild, "Expected the camp to trigger within 40 slots.");
            return anchor;
        }

        [Test]
        public void CampAnchor_AllocatesCampKind_WithCrewInTheAuthoredRange()
        {
            var catalog = new SiteCatalog(new[] { CampSite(crewMin: 2, crewMax: 4) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, Pool(5, 9));

            var anchor = FirstAnchor(allocator);

            Assert.AreEqual(WorldSlotKind.Camp, anchor.Kind);
            Assert.AreEqual("bandit-boss", anchor.Flavor, "The Camp slot's flavor is the boss story flavor.");
            Assert.GreaterOrEqual(anchor.CrewEnemyIds.Count, 2);
            Assert.LessOrEqual(anchor.CrewEnemyIds.Count, 4);
        }

        [Test]
        public void CampAnchor_SameSeed_ProducesIdenticalCrew()
        {
            var sites = new[] { CampSite() };
            var a = Allocator(Density(avgPerAmbientSite: 1), new SiteCatalog(sites), Pool(5, 9), seed: 99);
            var b = Allocator(Density(avgPerAmbientSite: 1), new SiteCatalog(sites), Pool(5, 9), seed: 99);

            var anchorA = FirstAnchor(a);
            var anchorB = FirstAnchor(b);

            Assert.AreEqual(WorldSlotKind.Camp, anchorA.Kind);
            CollectionAssert.AreEqual(anchorA.CrewEnemyIds, anchorB.CrewEnemyIds);
        }

        [Test]
        public void CampAnchor_EmptyPool_LandsTheBossWithNoCrew()
        {
            var catalog = new SiteCatalog(new[] { CampSite() });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, pools: null);

            var anchor = FirstAnchor(allocator);

            Assert.AreEqual(WorldSlotKind.Camp, anchor.Kind, "No pool: the boss still lands, crew-less.");
            Assert.IsEmpty(anchor.CrewEnemyIds);
        }

        [Test]
        public void NonBossSites_KeepThePlainCombatAnchor()
        {
            var catalog = new SiteCatalog(new[] { AmbientSite(footprint: 1) });
            var allocator = Allocator(Density(avgPerAmbientSite: 1), catalog, Pool(5));

            var anchor = FirstAnchor(allocator);

            Assert.AreEqual(WorldSlotKind.Combat, anchor.Kind);
            Assert.IsEmpty(anchor.CrewEnemyIds);
        }

        [Test]
        public void Catalog_CollectsBossStoryFlavors()
        {
            var catalog = new SiteCatalog(new[] { CampSite(), AmbientSite("lair") });

            CollectionAssert.Contains(catalog.BossStoryFlavors, "bandit-boss");
            Assert.AreEqual(1, catalog.BossStoryFlavors.Count);
        }

        [Test]
        public void Catalog_SplitsChannels_AndDerivesNpcFillFlavors()
        {
            var catalog = new SiteCatalog(new[] { QuestSite("city"), AmbientSite("lair") });

            Assert.AreEqual(1, catalog.QuestSites.Count);
            Assert.AreEqual(1, catalog.AmbientSites.Count);
            Assert.AreEqual("city", catalog.QuestSites[0].SiteId);
            Assert.AreEqual("lair", catalog.AmbientSites[0].SiteId);
            CollectionAssert.Contains(catalog.NpcFillFlavors, "townsfolk");
            CollectionAssert.DoesNotContain(catalog.NpcFillFlavors, "quest-bearer");
            Assert.IsNull(catalog.Get("atlantis"));
            Assert.IsNotNull(catalog.Get("city"));
        }
    }
}
