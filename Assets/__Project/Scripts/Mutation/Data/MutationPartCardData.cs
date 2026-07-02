using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Everything a mutation card face needs to show one body part: display name, choice
    /// icon, rarity tier (the potency glow driver), and the abilities the part grants
    /// (actives first, then passives — the order the combat resolver composes them in).
    /// Served by <see cref="IMutationPartCatalog.TryGetCardData"/> for any catalog part,
    /// so both the offered variant (front face) and the replaced part (back face) resolve
    /// through the same bridge.
    /// </summary>
    public sealed class MutationPartCardData
    {
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public int RarityTier { get; }
        public IReadOnlyList<MutationAbilityInfo> Abilities { get; }

        public MutationPartCardData(
            string displayName,
            Sprite icon,
            int rarityTier,
            IReadOnlyList<MutationAbilityInfo> abilities)
        {
            DisplayName = displayName;
            Icon = icon;
            RarityTier = rarityTier;
            Abilities = abilities ?? Array.Empty<MutationAbilityInfo>();
        }
    }
}
