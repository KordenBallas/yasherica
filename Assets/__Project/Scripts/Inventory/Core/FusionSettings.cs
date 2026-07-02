using System;

namespace Inventory.Core
{
    /// <summary>
    /// Tunables of the emergent fusion grammar, fed from InventoryConfig so the
    /// designer balances fusion without touching code (no magic numbers in the
    /// calculator or selector).
    /// </summary>
    public class FusionSettings
    {
        /// <summary>Tier bonus per trait that appears in two or more inputs (amplify).</summary>
        public int AmplifyTierBonus { get; }

        /// <summary>Score weight per trait shared between the target and a candidate.</summary>
        public float TraitOverlapWeight { get; }

        /// <summary>Score penalty per candidate trait absent from the target (dilution).</summary>
        public float TraitMismatchWeight { get; }

        /// <summary>Score penalty per point of tier distance between target and candidate.</summary>
        public float TierProximityWeight { get; }

        public FusionSettings(
            int amplifyTierBonus,
            float traitOverlapWeight,
            float traitMismatchWeight,
            float tierProximityWeight)
        {
            if (amplifyTierBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amplifyTierBonus));
            }

            if (traitOverlapWeight < 0f || traitMismatchWeight < 0f || tierProximityWeight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(traitOverlapWeight), "Fusion scoring weights must be non-negative.");
            }

            AmplifyTierBonus = amplifyTierBonus;
            TraitOverlapWeight = traitOverlapWeight;
            TraitMismatchWeight = traitMismatchWeight;
            TierProximityWeight = tierProximityWeight;
        }
    }
}
