using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Loot.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LootRollServiceTests
    {
        private const int RunSeed = 9001;
        private const float TagBias = 2f;

        private sealed class StubCatalog : IBiomeLootCatalog
        {
            private readonly Dictionary<LevelTheme, BiomeLootData> _biomes = new Dictionary<LevelTheme, BiomeLootData>();

            public StubCatalog With(BiomeLootData biome)
            {
                _biomes[biome.Theme] = biome;
                return this;
            }

            public BiomeLootData Get(LevelTheme theme)
            {
                return _biomes.TryGetValue(theme, out var biome) ? biome : null;
            }
        }

        private sealed class RejectAllFilter : ILootEntryFilter
        {
            public bool IsEligible(LootEntryData entry, LootRollContext context) => false;
        }

        private sealed class RejectByIdFilter : ILootEntryFilter
        {
            private readonly string _rejectedId;

            public RejectByIdFilter(string rejectedId)
            {
                _rejectedId = rejectedId;
            }

            public bool IsEligible(LootEntryData entry, LootRollContext context)
            {
                return entry.ArtifactId != _rejectedId;
            }
        }

        private static BiomeLootData ForestBiome(
            float platformChance = 1f,
            int platformCountMin = 1,
            int platformCountMax = 1,
            IReadOnlyList<LootEntryData> platformTable = null,
            float enemyDropChance = 1f,
            IReadOnlyList<LootEntryData> enemyDropTable = null,
            int questCountMin = 1,
            int questCountMax = 1,
            IReadOnlyList<LootEntryData> questTable = null)
        {
            return new BiomeLootData(
                LevelTheme.Forest,
                platformChance,
                platformCountMin,
                platformCountMax,
                platformTable ?? new[] { new LootEntryData("fire", 1f) },
                enemyDropChance,
                enemyDropTable ?? new[] { new LootEntryData("water", 1f) },
                questCountMin,
                questCountMax,
                questTable ?? new[] { new LootEntryData("rock", 1f) });
        }

        private static LootRollService CreateService(
            BiomeLootData biome,
            int runSeed = RunSeed,
            IReadOnlyList<ILootEntryFilter> filters = null,
            float tagBias = TagBias)
        {
            var seedProvider = new RunSeedProvider();
            seedProvider.SetSeed(runSeed);
            var catalog = new StubCatalog().With(biome);
            return new LootRollService(catalog, seedProvider, filters, tagBias);
        }

        private static LootRollContext Context(string key, IReadOnlyList<string> tags = null)
        {
            return new LootRollContext(LevelTheme.Forest, key, tags);
        }

        [Test]
        public void RollPlatformLoot_SameSeedAndContext_IsDeterministicAcrossCallsAndInstances()
        {
            var biome = ForestBiome(
                platformCountMin: 1,
                platformCountMax: 3,
                platformTable: new[]
                {
                    new LootEntryData("fire", 1f),
                    new LootEntryData("water", 1f),
                    new LootEntryData("rock", 1f)
                });
            var context = Context("platform:5");

            var first = CreateService(biome).RollPlatformLoot(context);
            var second = CreateService(biome).RollPlatformLoot(context);

            CollectionAssert.AreEqual(
                first.Select(r => r.ArtifactId).ToList(),
                second.Select(r => r.ArtifactId).ToList());
            Assert.AreEqual(first.Count, second.Count);
        }

        [Test]
        public void RollPlatformLoot_DifferentRunSeeds_ProduceDifferentResults()
        {
            var biome = ForestBiome(
                platformCountMin: 3,
                platformCountMax: 3,
                platformTable: new[]
                {
                    new LootEntryData("fire", 1f),
                    new LootEntryData("water", 1f),
                    new LootEntryData("rock", 1f),
                    new LootEntryData("snake", 1f)
                });
            var context = Context("platform:5");

            // A single differing roll is enough; check several run seeds to avoid
            // a flaky collision on one pair.
            var baseline = CreateService(biome, runSeed: 1).RollPlatformLoot(context)
                .Select(r => r.ArtifactId).ToList();
            var anyDiffers = Enumerable.Range(2, 5)
                .Select(seed => CreateService(biome, runSeed: seed).RollPlatformLoot(context)
                    .Select(r => r.ArtifactId).ToList())
                .Any(other => !other.SequenceEqual(baseline));

            Assert.IsTrue(anyDiffers);
        }

        [Test]
        public void RollPlatformLoot_UnknownBiome_ReturnsEmpty()
        {
            var service = CreateService(ForestBiome());
            var context = new LootRollContext(LevelTheme.Desert, "platform:1");

            Assert.IsEmpty(service.RollPlatformLoot(context));
        }

        [Test]
        public void RollPlatformLoot_CountRange_IsRespected()
        {
            var biome = ForestBiome(platformCountMin: 2, platformCountMax: 4);

            for (int i = 0; i < 20; i++)
            {
                var results = CreateService(biome).RollPlatformLoot(Context($"platform:{i}"));
                Assert.GreaterOrEqual(results.Count, 2);
                Assert.LessOrEqual(results.Count, 4);
            }
        }

        [Test]
        public void ShouldPlaceLootOnPlatform_HonorsZeroAndOneProbabilities()
        {
            var always = CreateService(ForestBiome(platformChance: 1f));
            var never = CreateService(ForestBiome(platformChance: 0f));

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(always.ShouldPlaceLootOnPlatform(LevelTheme.Forest, i));
                Assert.IsFalse(never.ShouldPlaceLootOnPlatform(LevelTheme.Forest, i));
            }
        }

        [Test]
        public void ShouldPlaceLootOnPlatform_UnknownBiome_ReturnsFalse()
        {
            var service = CreateService(ForestBiome(platformChance: 1f));

            Assert.IsFalse(service.ShouldPlaceLootOnPlatform(LevelTheme.Cave, 0));
        }

        [Test]
        public void RollEnemyDrops_SlotProbabilityEdges_AreHonored()
        {
            var service = CreateService(ForestBiome());
            var slots = new[]
            {
                new LootSlotData("fire", 1f, 1, 1, false),
                new LootSlotData("water", 0f, 1, 1, false)
            };

            for (int i = 0; i < 10; i++)
            {
                var results = service.RollEnemyDrops(slots, Context($"enemy:{i}:7"));
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual("fire", results[0].ArtifactId);
            }
        }

        [Test]
        public void RollEnemyDrops_QuantityRange_IsRespected()
        {
            var service = CreateService(ForestBiome());
            var slots = new[] { new LootSlotData("fire", 1f, 2, 5, false) };

            for (int i = 0; i < 20; i++)
            {
                var results = service.RollEnemyDrops(slots, Context($"enemy:{i}:7"));
                Assert.GreaterOrEqual(results[0].Quantity, 2);
                Assert.LessOrEqual(results[0].Quantity, 5);
            }
        }

        [Test]
        public void RollEnemyDrops_BonusFlag_IsPassedThrough()
        {
            var service = CreateService(ForestBiome());
            var slots = new[] { new LootSlotData("virus", 1f, 1, 1, true) };

            var results = service.RollEnemyDrops(slots, Context("enemy:1:7"));

            Assert.IsTrue(results[0].IsBonus);
        }

        [Test]
        public void RollEnemyDrops_EmptySlots_FallsBackToBiomeTable()
        {
            var service = CreateService(ForestBiome(
                enemyDropChance: 1f,
                enemyDropTable: new[] { new LootEntryData("water", 1f) }));

            var results = service.RollEnemyDrops(null, Context("enemy:1:7"));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("water", results[0].ArtifactId);
        }

        [Test]
        public void RollEnemyDrops_EmptySlots_BiomeChanceZero_DropsNothing()
        {
            var service = CreateService(ForestBiome(enemyDropChance: 0f));

            Assert.IsEmpty(service.RollEnemyDrops(new List<LootSlotData>(), Context("enemy:1:7")));
        }

        [Test]
        public void RollQuestRewards_SameContext_IsDeterministic()
        {
            var biome = ForestBiome(
                questCountMin: 1,
                questCountMax: 3,
                questTable: new[]
                {
                    new LootEntryData("fire", 1f),
                    new LootEntryData("rock", 1f),
                    new LootEntryData("snake", 1f)
                });
            var context = Context("quest:story_a:npc_b");

            var first = CreateService(biome).RollQuestRewards(context);
            var second = CreateService(biome).RollQuestRewards(context);

            CollectionAssert.AreEqual(
                first.Select(r => r.ArtifactId).ToList(),
                second.Select(r => r.ArtifactId).ToList());
        }

        [Test]
        public void RollQuestRewards_TagBias_FavorsMatchingEntries()
        {
            // With equal base weights and a large bias multiplier, the tagged entry
            // must win clearly more often across many distinct contexts.
            var biome = ForestBiome(
                questTable: new[]
                {
                    new LootEntryData("biased", 1f, new[] { "bandit" }),
                    new LootEntryData("plain", 1f)
                });
            var service = CreateService(biome, tagBias: 50f);
            var tags = new[] { "bandit" };

            int biasedCount = 0;
            const int total = 200;
            for (int i = 0; i < total; i++)
            {
                var results = service.RollQuestRewards(Context($"quest:story_{i}:npc", tags));
                if (results.Count > 0 && results[0].ArtifactId == "biased")
                {
                    biasedCount++;
                }
            }

            Assert.Greater(biasedCount, total * 3 / 4);
        }

        [Test]
        public void RollQuestRewards_NoMatchingTags_UsesBaseWeights()
        {
            var biome = ForestBiome(
                questTable: new[]
                {
                    new LootEntryData("biased", 0f, new[] { "bandit" }),
                    new LootEntryData("plain", 1f)
                });
            var service = CreateService(biome, tagBias: 50f);

            var results = service.RollQuestRewards(Context("quest:story:npc", new[] { "merchant" }));

            Assert.AreEqual("plain", results[0].ArtifactId);
        }

        [Test]
        public void Filters_RejectAll_YieldsEmptyResults()
        {
            var service = CreateService(ForestBiome(), filters: new[] { new RejectAllFilter() });

            Assert.IsEmpty(service.RollPlatformLoot(Context("platform:1")));
            Assert.IsEmpty(service.RollQuestRewards(Context("quest:a:b")));
        }

        [Test]
        public void Filters_RejectById_ExcludesOnlyThatEntry()
        {
            var biome = ForestBiome(
                questTable: new[]
                {
                    new LootEntryData("fire", 1f),
                    new LootEntryData("water", 1f)
                });
            var service = CreateService(
                biome,
                filters: new ILootEntryFilter[] { new RejectByIdFilter("fire") });

            for (int i = 0; i < 10; i++)
            {
                var results = service.RollQuestRewards(Context($"quest:story_{i}:npc"));
                Assert.AreEqual("water", results[0].ArtifactId);
            }
        }
    }
}
