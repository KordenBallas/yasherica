using System;

namespace Mutation.Core
{
    /// <summary>
    /// Tunables of the unseal variant scoring, sourced from MutationConfig so the
    /// designer balances the puzzle without touching code. Rarity gating rides the
    /// socketed potency: a part of rarity tier R fully unlocks once the socketed
    /// target tier reaches R x <see cref="TierUnlockPerRarityTier"/>.
    /// </summary>
    public readonly struct VariantScoringParameters
    {
        public float RarityWeight { get; }
        public float TierUnlockPerRarityTier { get; }

        public VariantScoringParameters(float rarityWeight, float tierUnlockPerRarityTier)
        {
            if (rarityWeight < 0f || tierUnlockPerRarityTier < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rarityWeight), "Variant scoring parameters must be non-negative.");
            }

            RarityWeight = rarityWeight;
            TierUnlockPerRarityTier = tierUnlockPerRarityTier;
        }
    }
}
