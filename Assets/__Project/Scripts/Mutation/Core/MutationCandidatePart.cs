using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// A body part the unseal variant menu can offer, described in UnityEngine-free terms so the
    /// scoring stays pure C# and unit-testable. Built by the Data layer from a character
    /// <c>PartDefinition</c>: the slot/part ids, a display label, the part's per-trait affinity
    /// (trait id -> weight, 0..1) scored against the socketed reagents, and the part's rarity as an
    /// int tier (Common = 0; higher = rarer). Rarity is carried as an int so Core takes no
    /// dependency on the character layer's rarity enum.
    /// </summary>
    public sealed class MutationCandidatePart
    {
        public string SlotId { get; }
        public string PartId { get; }
        public string DisplayName { get; }
        public int RarityTier { get; }

        /// <summary>Per-trait affinity (trait id -> weight) scored against the socketed reagents at unseal.</summary>
        public IReadOnlyDictionary<string, float> TraitAffinity { get; }

        public MutationCandidatePart(
            string slotId,
            string partId,
            string displayName,
            int rarityTier,
            IReadOnlyDictionary<string, float> traitAffinity)
        {
            SlotId = slotId;
            PartId = partId;
            DisplayName = displayName;
            RarityTier = rarityTier;
            TraitAffinity = traitAffinity ?? EmptyAffinity;
        }

        private static readonly IReadOnlyDictionary<string, float> EmptyAffinity =
            new Dictionary<string, float>(0);
    }
}
