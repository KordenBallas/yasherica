using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using Narrative.Director.Core;

namespace LevelGeneration.Journey
{
    /// <summary>
    /// Seeded, tier-climbing biome itinerary (brief: biome-selection-along-the-run). Stretch s draws
    /// its biome from the s-th lowest authored tier (clamped at the top — the run keeps serving
    /// top-tier biomes; there is no apex yet) via a weighted pick over that tier's positive-weight
    /// entries, excluding the previous stretch's biome whenever the tier offers an alternative so a
    /// stretch boundary is a visible crossing. Lazily extends and caches its stretch plan; draws are
    /// consumed strictly in stretch order, so <see cref="ForWindow"/> is idempotent and
    /// order-independent (determinism is structural). Owns its own random stream — interleaving with
    /// the shared narrative stream would reshuffle story/enemy picks whenever a stretch boundary moved.
    /// </summary>
    public sealed class BiomeJourney : IBiomeJourney
    {
        private readonly IReadOnlyList<BiomeProgressionEntry> _entries;
        private readonly IReadOnlyList<int> _tiers;
        private readonly IRandomSource _random;
        private readonly List<BiomeStretch> _stretches = new List<BiomeStretch>();

        public BiomeJourney(BiomeProgressionSettings settings, IRandomSource random, IGameLogger logger = null)
        {
            _random = random;

            var entries = settings?.Entries;
            if (entries == null || !entries.Any(e => e.SelectionWeight > 0))
            {
                // Fail-safe: an empty/weightless roster must not take the run down — fall back to the
                // pre-journey fixed-Forest behavior and say so once.
                logger?.Warning(LogCategory.LevelGeneration,
                    "[BiomeJourney] No biome progression entries with positive weight; falling back to fixed Forest (tier 1).");
                entries = BiomeProgressionSettings.CreateDefault().Entries;
            }

            _entries = entries;
            _tiers = entries
                .Where(e => e.SelectionWeight > 0)
                .Select(e => e.EscalationTier)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        public BiomeStretch ForWindow(int windowIndex)
        {
            if (windowIndex < 0)
            {
                windowIndex = 0;
            }

            while (_stretches.Count == 0 || _stretches[_stretches.Count - 1].EndWindowExclusive <= windowIndex)
            {
                AppendNextStretch();
            }

            // Stretches partition the window axis contiguously from 0; find the covering one. The
            // common callers ask for the latest window, so scan from the end.
            for (int i = _stretches.Count - 1; i >= 0; i--)
            {
                if (_stretches[i].FirstWindow <= windowIndex)
                {
                    return _stretches[i];
                }
            }

            return _stretches[0];
        }

        private void AppendNextStretch()
        {
            int stretchIndex = _stretches.Count;
            int tierRank = stretchIndex < _tiers.Count ? stretchIndex : _tiers.Count - 1;
            int tier = _tiers[tierRank];

            var pool = new List<BiomeProgressionEntry>();
            foreach (var entry in _entries)
            {
                if (entry.EscalationTier == tier && entry.SelectionWeight > 0)
                {
                    pool.Add(entry);
                }
            }

            // A boundary that re-picks the same biome is an invisible non-event; exclude the previous
            // biome when the tier offers an alternative. A single-biome tier legitimately repeats.
            if (stretchIndex > 0 && pool.Count > 1)
            {
                LevelTheme previous = _stretches[stretchIndex - 1].Theme;
                pool.RemoveAll(e => e.Theme == previous);
                if (pool.Count == 0)
                {
                    // All alternatives shared the previous theme (duplicate-theme authoring); undo.
                    pool.AddRange(_entries.Where(e => e.EscalationTier == tier && e.SelectionWeight > 0));
                }
            }

            BiomeProgressionEntry picked = PickWeighted(pool);
            int windowCount = picked.StretchMinWindows
                + _random.NextInt(picked.StretchMaxWindows - picked.StretchMinWindows + 1);
            int firstWindow = stretchIndex == 0 ? 0 : _stretches[stretchIndex - 1].EndWindowExclusive;

            _stretches.Add(new BiomeStretch(picked.Theme, tier, stretchIndex, firstWindow, windowCount));
        }

        private BiomeProgressionEntry PickWeighted(IReadOnlyList<BiomeProgressionEntry> pool)
        {
            int total = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                total += pool[i].SelectionWeight;
            }

            int roll = _random.NextInt(total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].SelectionWeight;
                if (roll < 0)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }
    }
}
