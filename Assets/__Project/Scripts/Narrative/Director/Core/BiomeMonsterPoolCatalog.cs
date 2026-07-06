using System;
using System.Collections.Generic;
using LevelGeneration;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Plain dictionary-backed <see cref="IBiomeMonsterPoolCatalog"/>. The data layer
    /// (<c>BiomeMonsterPoolMapper</c>) converts the authored SO assets into the tagged entry map once
    /// at install time so this stays UnityEngine-free and unit-testable. Flavor filtering compares
    /// tags case-insensitively (matching the loot-table tag convention); the ids-only constructor is
    /// kept for callers that need no flavors (every entry untagged).
    /// </summary>
    public sealed class BiomeMonsterPoolCatalog : IBiomeMonsterPoolCatalog
    {
        private static readonly int[] EmptyPool = Array.Empty<int>();

        private readonly Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>> _pools;
        private readonly Dictionary<LevelTheme, IReadOnlyList<int>> _idsByTheme =
            new Dictionary<LevelTheme, IReadOnlyList<int>>();

        public BiomeMonsterPoolCatalog(Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>> pools)
        {
            _pools = pools ?? new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>();
            foreach (var pair in _pools)
            {
                var ids = new int[pair.Value?.Count ?? 0];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = pair.Value[i].Id;
                }

                _idsByTheme[pair.Key] = ids;
            }
        }

        public BiomeMonsterPoolCatalog(Dictionary<LevelTheme, IReadOnlyList<int>> pools)
            : this(ToUntaggedEntries(pools))
        {
        }

        public IReadOnlyList<int> GetPool(LevelTheme theme)
        {
            return _idsByTheme.TryGetValue(theme, out var ids) ? ids : EmptyPool;
        }

        public IReadOnlyList<int> GetPool(LevelTheme theme, string flavor)
        {
            if (string.IsNullOrEmpty(flavor))
            {
                return GetPool(theme);
            }

            if (!_pools.TryGetValue(theme, out var entries) || entries == null)
            {
                return EmptyPool;
            }

            var matched = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (HasTag(entries[i].Tags, flavor))
                {
                    matched.Add(entries[i].Id);
                }
            }

            return matched;
        }

        public IReadOnlyList<int> GetPool(LevelTheme theme, int tier)
        {
            if (!_pools.TryGetValue(theme, out var entries) || entries == null)
            {
                return EmptyPool;
            }

            var matched = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Band.Contains(tier))
                {
                    matched.Add(entries[i].Id);
                }
            }

            return matched;
        }

        public IReadOnlyList<int> GetPool(LevelTheme theme, string flavor, int tier)
        {
            if (string.IsNullOrEmpty(flavor))
            {
                return GetPool(theme, tier);
            }

            if (!_pools.TryGetValue(theme, out var entries) || entries == null)
            {
                return EmptyPool;
            }

            var matched = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (HasTag(entries[i].Tags, flavor) && entries[i].Band.Contains(tier))
                {
                    matched.Add(entries[i].Id);
                }
            }

            return matched;
        }

        private static bool HasTag(IReadOnlyList<string> tags, string flavor)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], flavor, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>> ToUntaggedEntries(
            Dictionary<LevelTheme, IReadOnlyList<int>> pools)
        {
            var result = new Dictionary<LevelTheme, IReadOnlyList<MonsterPoolEntry>>();
            if (pools == null)
            {
                return result;
            }

            foreach (var pair in pools)
            {
                var entries = new MonsterPoolEntry[pair.Value?.Count ?? 0];
                for (int i = 0; i < entries.Length; i++)
                {
                    entries[i] = new MonsterPoolEntry(pair.Value[i], null);
                }

                result[pair.Key] = entries;
            }

            return result;
        }
    }
}
