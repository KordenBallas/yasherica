using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using LevelGeneration;
using Loot.Core;
using Narrative.Director.Core;
using NUnit.Framework;
using Sites = World.Sites.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// Acceptance-shaped seeded histogram over the authored-equivalent site vocabulary (the
    /// world-sites brief's criteria): most platforms are Wild; sites are rare contiguous blocks;
    /// a City is busier (more townsfolk/market/guard fills) than a Village; landmarks carry no
    /// townsfolk; and the same seed reproduces the same world.
    /// </summary>
    [TestFixture]
    public class WorldSitesAcceptanceTests
    {
        private const int RunLength = 600;

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

        private static Sites.ContentBeat Beat(Sites.ContentBaseKind kind, string flavor) =>
            new Sites.ContentBeat(kind, flavor);

        private static Sites.WeightedBeat Row(Sites.ContentBaseKind kind, string flavor, int weight) =>
            new Sites.WeightedBeat(Beat(kind, flavor), weight);

        /// <summary>The authored vocabulary mirrored as effective records (per the shipped assets).</summary>
        private static Sites.SiteCatalog AuthoredCatalog() => new Sites.SiteCatalog(new[]
        {
            new Sites.SiteDefinitionData("village", "settlement", 2, 3, 40,
                new[] { Beat(Sites.ContentBaseKind.Npc, "quest-bearer") }, 1, 1,
                new[] { Row(Sites.ContentBaseKind.Npc, "townsfolk", 3), Row(Sites.ContentBaseKind.Loot, "scattered", 1) },
                "village-kit"),
            new Sites.SiteDefinitionData("city", "settlement", 4, 5, 20,
                new[] { Beat(Sites.ContentBaseKind.Npc, "quest-bearer") }, 2, 3,
                new[]
                {
                    Row(Sites.ContentBaseKind.Npc, "townsfolk", 5),
                    Row(Sites.ContentBaseKind.Loot, "market", 3),
                    Row(Sites.ContentBaseKind.Combat, "guard", 2)
                },
                "city-kit"),
            new Sites.SiteDefinitionData("camp", "settlement", 1, 2, 3,
                new[] { Beat(Sites.ContentBaseKind.Combat, "bandit") }, 0, 1,
                new[] { Row(Sites.ContentBaseKind.Loot, "stash", 1) }, "camp-kit"),
            new Sites.SiteDefinitionData("ruin", "landmark", 1, 2, 3,
                new[] { Beat(Sites.ContentBaseKind.Loot, "relic") }, 0, 1,
                new[] { Row(Sites.ContentBaseKind.Combat, "den-monster", 2), Row(Sites.ContentBaseKind.Loot, "scattered", 1) },
                "ruin-kit"),
            new Sites.SiteDefinitionData("lair", "landmark", 1, 2, 3,
                new[] { Beat(Sites.ContentBaseKind.Combat, "den-monster") }, 0, 1,
                new[] { Row(Sites.ContentBaseKind.Combat, "den-monster", 2), Row(Sites.ContentBaseKind.Loot, "scattered", 1) },
                "lair-kit")
        });

        /// <summary>The shipped density defaults (quest 1-in-10 behind spacing 4; sites 1-in-14/6/40).</summary>
        private static readonly WorldContentDensitySettings ShippedDensity =
            new WorldContentDensitySettings(10, 4, 65, 15, 20, 14, 6, 40);

        private static IBiomeMonsterPoolCatalog TaggedPool() =>
            new BiomeMonsterPoolCatalog(new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>
            {
                {
                    LevelTheme.Forest, new[]
                    {
                        new MonsterPoolEntry(1, new[] { "wild-beast", "den-monster" }),
                        new MonsterPoolEntry(9001, new[] { "bandit", "guard" })
                    }
                }
            });

        /// <summary>One allocated run: every slot, with settlements reserved on each landed quest.</summary>
        private static List<SlotAllocation> Allocate(ulong seed)
        {
            var random = new DeterministicRandom(seed);
            var pools = TaggedPool();
            var themes = new FakeThemeProvider();
            var inner = new WorldContentAllocator(ShippedDensity, pools, themes, random, new FakeLogger());
            var allocator = new SiteAwareSlotAllocator(inner, AuthoredCatalog(), new Sites.SiteBlockBuilder(),
                ShippedDensity, pools, themes, random, new FakeLogger());

            var slots = new List<SlotAllocation>(RunLength);
            for (int i = 0; i < RunLength; i++)
            {
                var slot = allocator.AllocateSlot(questAvailable: true);
                if (slot.Kind == WorldSlotKind.Quest)
                {
                    var stamp = allocator.TryReserveSettlement(null);
                    // Re-wrap so the histogram sees the quest platform's stamp (as the planner does).
                    slot = new SlotAllocation(WorldSlotKind.Quest, 0, null, stamp);
                }

                slots.Add(slot);
            }

            return slots;
        }

        /// <summary>Groups site slots into blocks by (siteId, instanceId).</summary>
        private static Dictionary<(string, int), List<SlotAllocation>> Blocks(List<SlotAllocation> slots) =>
            slots.Where(s => !s.Site.IsWild)
                .GroupBy(s => (s.Site.SiteId, s.Site.InstanceId))
                .ToDictionary(g => g.Key, g => g.ToList());

        [Test]
        public void WildMajority_AndSitesAreRareContiguousBlocks()
        {
            var slots = Allocate(seed: 20260703);

            int wild = slots.Count(s => s.Site.IsWild);
            Assert.Greater(wild / (float)RunLength, 0.6f, "Most of the world must stay Wild.");

            var blocks = Blocks(slots);
            Assert.Greater(blocks.Count, 3, "Expected several sites over a long run.");
            Assert.Less(blocks.Count, RunLength / 10, "Sites must be rare events.");

            foreach (var block in blocks)
            {
                var indices = block.Value.Select(s => s.Site.Index).OrderBy(i => i).ToList();
                Assert.AreEqual(block.Value[0].Site.Footprint, indices.Count,
                    $"Block {block.Key} is incomplete.");
                CollectionAssert.AreEqual(Enumerable.Range(0, indices.Count), indices,
                    $"Block {block.Key} is not a contiguous 0..N-1 run.");
            }

            // Both trigger channels fire over a long run.
            Assert.IsTrue(blocks.Keys.Any(k => k.Item1 == "village" || k.Item1 == "city"),
                "Expected at least one quest-pulled settlement.");
            Assert.IsTrue(blocks.Keys.Any(k => k.Item1 == "camp" || k.Item1 == "ruin" || k.Item1 == "lair"),
                "Expected at least one ambient-channel site.");
        }

        [Test]
        public void City_IsBusierThanVillage_AndLandmarksHaveNoTownsfolk()
        {
            // Aggregate across several seeds so at least one city occurs and averages are stable.
            var cityFills = new List<int>();
            var villageFills = new List<int>();
            for (ulong seed = 1; seed <= 12; seed++)
            {
                var blocks = Blocks(Allocate(seed));
                foreach (var block in blocks)
                {
                    int beats = block.Value.Count(s => s.Site.Index > 0 && s.Kind != WorldSlotKind.Empty);
                    if (block.Key.Item1 == "city") { cityFills.Add(beats); }
                    if (block.Key.Item1 == "village") { villageFills.Add(beats); }

                    if (block.Key.Item1 == "ruin" || block.Key.Item1 == "lair")
                    {
                        Assert.IsFalse(block.Value.Any(s => s.Kind == WorldSlotKind.Npc),
                            $"Landmark {block.Key} must have no townsfolk.");
                    }

                    if (block.Key.Item1 == "city")
                    {
                        // Guard fights draw the guard-tagged enemy from the pool.
                        foreach (var slot in block.Value.Where(s => s.Kind == WorldSlotKind.Combat))
                        {
                            Assert.AreEqual(9001, slot.EnemyId, "City guard fight must pick the tagged enemy.");
                        }
                    }
                }
            }

            Assert.IsNotEmpty(cityFills, "Expected at least one city across 12 seeds.");
            Assert.IsNotEmpty(villageFills, "Expected at least one village across 12 seeds.");
            Assert.Greater(cityFills.Average(), villageFills.Average(),
                "A city must carry more secondary beats than a village.");
        }

        [Test]
        public void SameSeed_ReproducesTheSameWorld()
        {
            var a = Allocate(seed: 777);
            var b = Allocate(seed: 777);

            for (int i = 0; i < RunLength; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind, $"Kind diverged at slot {i}.");
                Assert.AreEqual(a[i].EnemyId, b[i].EnemyId, $"EnemyId diverged at slot {i}.");
                Assert.AreEqual(a[i].Flavor, b[i].Flavor, $"Flavor diverged at slot {i}.");
                Assert.AreEqual(a[i].Site.SiteId, b[i].Site.SiteId, $"Site diverged at slot {i}.");
                Assert.AreEqual(a[i].Site.InstanceId, b[i].Site.InstanceId, $"Instance diverged at slot {i}.");
            }
        }
    }
}
