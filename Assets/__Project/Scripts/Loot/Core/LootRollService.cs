using System;
using System.Collections.Generic;
using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Rolls loot from biome tables and enemy slots. Every public roll derives its
    /// own Random from (runSeed, contextKey) so outcomes are reproducible within a
    /// run and independent of roll order.
    /// </summary>
    public class LootRollService : ILootRollService
    {
        private const string PlatformPresenceKeyPrefix = "platform-presence:";

        private readonly IBiomeLootCatalog _catalog;
        private readonly IRunSeedProvider _seedProvider;
        private readonly IReadOnlyList<ILootEntryFilter> _filters;
        private readonly float _tagBiasMultiplier;

        public LootRollService(
            IBiomeLootCatalog catalog,
            IRunSeedProvider seedProvider,
            IReadOnlyList<ILootEntryFilter> filters,
            float tagBiasMultiplier)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _seedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));
            _filters = filters ?? Array.Empty<ILootEntryFilter>();
            _tagBiasMultiplier = tagBiasMultiplier;
        }

        public bool ShouldPlaceLootOnPlatform(LevelTheme theme, int platformIndex)
        {
            var biome = _catalog.Get(theme);
            if (biome == null)
            {
                return false;
            }

            var random = CreateRandom(PlatformPresenceKeyPrefix + platformIndex);
            return random.NextDouble() < biome.PlatformLootChance;
        }

        public IReadOnlyList<LootRollResult> RollPlatformLoot(LootRollContext context)
        {
            var biome = _catalog.Get(context.Theme);
            if (biome == null)
            {
                return Array.Empty<LootRollResult>();
            }

            var random = CreateRandom(context.ContextKey);
            return RollFromTable(
                biome.PlatformTable,
                biome.PlatformLootCountMin,
                biome.PlatformLootCountMax,
                context,
                random);
        }

        public IReadOnlyList<LootRollResult> RollEnemyDrops(
            IReadOnlyList<LootSlotData> slots,
            LootRollContext context)
        {
            var random = CreateRandom(context.ContextKey);

            if (slots == null || slots.Count == 0)
            {
                return RollBiomeFallbackDrop(context, random);
            }

            var results = new List<LootRollResult>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (random.NextDouble() > slot.Probability)
                {
                    continue;
                }

                var quantity = NextQuantity(random, slot.QuantityMin, slot.QuantityMax);
                results.Add(new LootRollResult(slot.ArtifactId, quantity, slot.IsBonus));
            }

            return results;
        }

        public IReadOnlyList<LootRollResult> RollQuestRewards(LootRollContext context)
        {
            var biome = _catalog.Get(context.Theme);
            if (biome == null)
            {
                return Array.Empty<LootRollResult>();
            }

            var random = CreateRandom(context.ContextKey);
            return RollFromTable(
                biome.QuestTable,
                biome.QuestRewardCountMin,
                biome.QuestRewardCountMax,
                context,
                random);
        }

        private IReadOnlyList<LootRollResult> RollBiomeFallbackDrop(LootRollContext context, Random random)
        {
            var biome = _catalog.Get(context.Theme);
            if (biome == null)
            {
                return Array.Empty<LootRollResult>();
            }

            if (random.NextDouble() > biome.EnemyDropChance)
            {
                return Array.Empty<LootRollResult>();
            }

            var eligible = FilterEligible(biome.EnemyDropTable, context);
            var entry = WeightedPicker.Pick(eligible, e => EffectiveWeight(e, context), random);
            if (entry == null)
            {
                return Array.Empty<LootRollResult>();
            }

            return new[] { new LootRollResult(entry.ArtifactId, 1, false) };
        }

        private IReadOnlyList<LootRollResult> RollFromTable(
            IReadOnlyList<LootEntryData> table,
            int countMin,
            int countMax,
            LootRollContext context,
            Random random)
        {
            var eligible = FilterEligible(table, context);
            if (eligible.Count == 0)
            {
                return Array.Empty<LootRollResult>();
            }

            var count = NextQuantity(random, countMin, countMax);
            var results = new List<LootRollResult>(count);
            for (int i = 0; i < count; i++)
            {
                var entry = WeightedPicker.Pick(eligible, e => EffectiveWeight(e, context), random);
                if (entry == null)
                {
                    break;
                }

                results.Add(new LootRollResult(entry.ArtifactId, 1, false));
            }

            return results;
        }

        private IReadOnlyList<LootEntryData> FilterEligible(
            IReadOnlyList<LootEntryData> table,
            LootRollContext context)
        {
            if (table == null || table.Count == 0)
            {
                return Array.Empty<LootEntryData>();
            }

            if (_filters.Count == 0)
            {
                return table;
            }

            var eligible = new List<LootEntryData>(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                if (IsEligible(table[i], context))
                {
                    eligible.Add(table[i]);
                }
            }

            return eligible;
        }

        private bool IsEligible(LootEntryData entry, LootRollContext context)
        {
            for (int i = 0; i < _filters.Count; i++)
            {
                if (!_filters[i].IsEligible(entry, context))
                {
                    return false;
                }
            }

            return true;
        }

        private float EffectiveWeight(LootEntryData entry, LootRollContext context)
        {
            return HasTagOverlap(entry.BiasTags, context.Tags)
                ? entry.Weight * _tagBiasMultiplier
                : entry.Weight;
        }

        private static bool HasTagOverlap(IReadOnlyList<string> entryTags, IReadOnlyList<string> contextTags)
        {
            if (entryTags.Count == 0 || contextTags.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < entryTags.Count; i++)
            {
                for (int j = 0; j < contextTags.Count; j++)
                {
                    if (string.Equals(entryTags[i], contextTags[j], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Random CreateRandom(string contextKey)
        {
            return new Random(LootSeed.Derive(_seedProvider.RunSeed, contextKey));
        }

        private static int NextQuantity(Random random, int min, int max)
        {
            if (min >= max)
            {
                return min;
            }

            return random.Next(min, max + 1);
        }
    }
}
