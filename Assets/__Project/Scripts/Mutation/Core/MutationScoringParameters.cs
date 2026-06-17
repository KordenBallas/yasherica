namespace Mutation.Core
{
    /// <summary>
    /// Tunables for the mutation scoring function, sourced from <c>MutationConfig</c> so balance lives
    /// in authored data, not code (CLAUDE.md §11). UnityEngine-free so the scoring stays unit-testable.
    ///
    /// A candidate part's score is <c>(affinity·tally) * (1 + RarityWeight * rarityTier * unlock)</c>,
    /// where <c>unlock</c> ramps from 0 to 1 as the stage's accumulated archetype points approach
    /// <c>rarityTier * RarityUnlockPointsPerTier</c>. So a rarer part's multiplier only grows once the
    /// player has fed enough, letting a slightly-lower-affinity rare part overtake a common one as the
    /// stage progresses. A part with no affinity to anything fed scores zero and is never offered.
    /// </summary>
    public readonly struct MutationScoringParameters
    {
        /// <summary>How strongly a part's rarity tier multiplies its score once unlocked.</summary>
        public float RarityWeight { get; }

        /// <summary>Accumulated archetype points required per rarity tier before that tier is favoured.</summary>
        public float RarityUnlockPointsPerTier { get; }

        public MutationScoringParameters(float rarityWeight, float rarityUnlockPointsPerTier)
        {
            RarityWeight = rarityWeight;
            RarityUnlockPointsPerTier = rarityUnlockPointsPerTier;
        }
    }
}
