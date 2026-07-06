namespace Narrative.Director.Core
{
    /// <summary>
    /// The run-escalation eligibility band for a placeable content item (a story or a monster): the
    /// range of run altitude tiers (<c>run_escalation_tier</c>, 1-based) at which the item belongs.
    /// The director draws an item only when the current tier falls inside its band (D19 escalation).
    ///
    /// Convention: <see cref="MinTier"/> is the primary gate — content <b>opens upward</b> from it.
    /// <see cref="MaxTier"/> is optional; a value <c>&lt;= 0</c> means "no upper bound", used sparingly
    /// to age out content that would break register high up. The default <c>(0, 0)</c> therefore means
    /// "eligible at every tier", so unbanded authored content keeps working with no migration (FR12).
    ///
    /// A pure, shared value object with no logic beyond the band predicate: escalation is the director's
    /// concern, but the band is referenced by both the story record and the monster-pool entry.
    /// </summary>
    public readonly struct RunTierBand
    {
        /// <summary>An open band eligible at every tier (the unbanded default).</summary>
        public static readonly RunTierBand Any = new RunTierBand(0, 0);

        public RunTierBand(int minTier, int maxTier)
        {
            MinTier = minTier;
            MaxTier = maxTier;
        }

        /// <summary>The lowest tier at which the content becomes eligible (opens upward).</summary>
        public int MinTier { get; }

        /// <summary>The highest eligible tier; <c>&lt;= 0</c> means no upper bound.</summary>
        public int MaxTier { get; }

        /// <summary>True when <paramref name="tier"/> falls within this band.</summary>
        public bool Contains(int tier)
        {
            return tier >= MinTier && (MaxTier <= 0 || tier <= MaxTier);
        }
    }
}
