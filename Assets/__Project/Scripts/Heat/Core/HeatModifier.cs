using System.Collections.Generic;

namespace Heat.Core
{
    /// <summary>
    /// One independent rules-modifier on the Heat menu (heat-ascension FR1/FR3): an id, a display
    /// name, the code seam it pulls (<see cref="HeatEffectKind"/>), and its authored rank steps.
    /// Rank 0 = not taken; rank k applies the summed magnitude of steps 1..k.
    /// </summary>
    public sealed class HeatModifier
    {
        private static readonly IReadOnlyList<HeatRank> EmptyRanks = new HeatRank[0];

        public string Id { get; }
        public string DisplayName { get; }
        public HeatEffectKind Kind { get; }
        public IReadOnlyList<HeatRank> Ranks { get; }

        /// <summary>The highest takeable rank (= the number of authored rank steps).</summary>
        public int MaxRank => Ranks.Count;

        public HeatModifier(string id, string displayName, HeatEffectKind kind, IReadOnlyList<HeatRank> ranks)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Ranks = ranks ?? EmptyRanks;
        }

        /// <summary>Total Heat of taking this modifier at <paramref name="rank"/> (steps 1..rank summed).</summary>
        public int HeatAtRank(int rank)
        {
            int clamped = ClampRank(rank);
            int total = 0;
            for (int i = 0; i < clamped; i++)
            {
                total += Ranks[i].HeatValue;
            }

            return total;
        }

        /// <summary>Total effect magnitude at <paramref name="rank"/> (steps 1..rank summed).</summary>
        public int MagnitudeAtRank(int rank)
        {
            int clamped = ClampRank(rank);
            int total = 0;
            for (int i = 0; i < clamped; i++)
            {
                total += Ranks[i].Magnitude;
            }

            return total;
        }

        public int ClampRank(int rank)
        {
            if (rank < 0)
            {
                return 0;
            }

            return rank > MaxRank ? MaxRank : rank;
        }
    }
}
