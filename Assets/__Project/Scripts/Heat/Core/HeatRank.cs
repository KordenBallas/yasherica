namespace Heat.Core
{
    /// <summary>
    /// One rank step of a Heat modifier (heat-ascension FR1): what taking this step adds to the
    /// pact's total Heat, how much it adds to the modifier's effect magnitude, and the rule text the
    /// player reads. Both numbers are per-step contributions — a pact at rank k sums steps 1..k.
    /// </summary>
    public sealed class HeatRank
    {
        /// <summary>Heat this step contributes to the pact total (per-step, not cumulative-authored).</summary>
        public int HeatValue { get; }

        /// <summary>Effect magnitude this step contributes (per-step; meaning is per <see cref="HeatEffectKind"/>).</summary>
        public int Magnitude { get; }

        /// <summary>The rule text for this step, in the player's language of pain.</summary>
        public string Description { get; }

        public HeatRank(int heatValue, int magnitude, string description)
        {
            HeatValue = heatValue < 0 ? 0 : heatValue;
            Magnitude = magnitude < 0 ? 0 : magnitude;
            Description = description ?? string.Empty;
        }
    }
}
