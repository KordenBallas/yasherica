using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View-layer DTO for one offered mutation card. The presenter resolves everything the
    /// card shows — the front face (offered part + its abilities), the back face (the part
    /// it would replace; <see cref="HasReplacedPart"/> false renders a "nothing replaced"
    /// back), the belonging tint (the blank's species archetype), and the rarity tier that
    /// drives the potency glow — so the view never sees Core or ScriptableObject types.
    /// SlotId/PartId let the view request the mini-model preview for this variant.
    /// </summary>
    public readonly struct MutationChoiceViewData
    {
        public string SlotId { get; }
        public string PartId { get; }
        public MutationCardFaceViewData Front { get; }
        public bool HasReplacedPart { get; }
        public MutationCardFaceViewData Back { get; }
        public Color Tint { get; }
        public int RarityTier { get; }

        public MutationChoiceViewData(
            string slotId,
            string partId,
            MutationCardFaceViewData front,
            bool hasReplacedPart,
            MutationCardFaceViewData back,
            Color tint,
            int rarityTier)
        {
            SlotId = slotId;
            PartId = partId;
            Front = front;
            HasReplacedPart = hasReplacedPart;
            Back = back;
            Tint = tint;
            RarityTier = rarityTier;
        }
    }
}
