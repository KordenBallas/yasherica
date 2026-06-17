using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// A body part the stage-up mutation choice can offer, described in UnityEngine-free terms so the
    /// scoring stays pure C# and unit-testable. Built by the Data layer from a character
    /// <c>PartDefinition</c>: the slot/part ids, a display label, the part's per-archetype affinity
    /// (archetype id -> weight, 0..1), the part's rarity as an int tier (Common = 0; higher = rarer),
    /// and the id of the archetype this part leans toward most (for the choice-button tint only).
    /// Rarity is carried as an int so Core takes no dependency on the character layer's rarity enum.
    /// </summary>
    public sealed class MutationCandidatePart
    {
        public string SlotId { get; }
        public string PartId { get; }
        public string DisplayName { get; }
        public IReadOnlyDictionary<string, float> Affinity { get; }
        public int RarityTier { get; }
        public string DominantArchetypeId { get; }

        public MutationCandidatePart(
            string slotId,
            string partId,
            string displayName,
            IReadOnlyDictionary<string, float> affinity,
            int rarityTier,
            string dominantArchetypeId)
        {
            SlotId = slotId;
            PartId = partId;
            DisplayName = displayName;
            Affinity = affinity ?? EmptyAffinity;
            RarityTier = rarityTier;
            DominantArchetypeId = dominantArchetypeId;
        }

        private static readonly IReadOnlyDictionary<string, float> EmptyAffinity =
            new Dictionary<string, float>(0);
    }
}
