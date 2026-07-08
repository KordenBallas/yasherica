using System.Collections.Generic;

namespace Heat.Core
{
    /// <summary>
    /// The sealed per-run pact (heat-ascension FR1/FR2): which modifiers were taken at which rank,
    /// and the resulting total Heat. Immutable and run-scoped — it rides the Continue image and is
    /// consumed on death. Built through <see cref="From"/>, which degrades gracefully (FR12): entries
    /// naming an unknown modifier are dropped, ranks clamp to the authored range — so a stale save
    /// against a re-authored menu yields a valid, possibly cooler pact, never a crash.
    /// </summary>
    public sealed class HeatPact
    {
        public readonly struct Entry
        {
            public string ModifierId { get; }
            public int Rank { get; }

            public Entry(string modifierId, int rank)
            {
                ModifierId = modifierId ?? string.Empty;
                Rank = rank;
            }
        }

        private static readonly IReadOnlyList<Entry> EmptyEntries = new Entry[0];

        /// <summary>The bare pact — Heat 0, today's game.</summary>
        public static readonly HeatPact None = new HeatPact(null, 0);

        /// <summary>Taken modifiers at their rank (rank ≥ 1; untaken modifiers are absent).</summary>
        public IReadOnlyList<Entry> Ranks { get; }

        /// <summary>The run's total Heat — the sum of every taken rank step's Heat value (FR1).</summary>
        public int TotalHeat { get; }

        private HeatPact(IReadOnlyList<Entry> ranks, int totalHeat)
        {
            Ranks = ranks ?? EmptyEntries;
            TotalHeat = totalHeat < 0 ? 0 : totalHeat;
        }

        /// <summary>The rank this pact holds for <paramref name="modifierId"/> (0 = not taken).</summary>
        public int RankOf(string modifierId)
        {
            for (int i = 0; i < Ranks.Count; i++)
            {
                if (Ranks[i].ModifierId == modifierId)
                {
                    return Ranks[i].Rank;
                }
            }

            return 0;
        }

        /// <summary>
        /// Builds a pact from raw (modifierId, rank) entries against the authored menu. Unknown ids
        /// drop, ranks clamp, rank-0/duplicate entries collapse (first wins) — the total is computed
        /// here and only here, so persisted pacts can never carry a stale total.
        /// </summary>
        public static HeatPact From(HeatSettings settings, IEnumerable<KeyValuePair<string, int>> entries)
        {
            if (settings == null || entries == null)
            {
                return None;
            }

            var taken = new List<Entry>();
            var seen = new HashSet<string>();
            int total = 0;
            foreach (var entry in entries)
            {
                if (entry.Key == null || !seen.Add(entry.Key))
                {
                    continue;
                }

                if (!settings.TryGetModifier(entry.Key, out var modifier))
                {
                    continue;
                }

                int rank = modifier.ClampRank(entry.Value);
                if (rank <= 0)
                {
                    continue;
                }

                taken.Add(new Entry(modifier.Id, rank));
                total += modifier.HeatAtRank(rank);
            }

            return taken.Count == 0 ? None : new HeatPact(taken, total);
        }
    }
}
