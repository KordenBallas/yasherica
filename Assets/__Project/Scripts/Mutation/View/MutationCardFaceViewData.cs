using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View-layer DTO for one face of a mutation card: the pictured part (name + image)
    /// and the ability icons beneath it. The front face shows the offered variant, the
    /// back face the part it would replace.
    /// </summary>
    public readonly struct MutationCardFaceViewData
    {
        public string PartName { get; }
        public Sprite PartIcon { get; }
        public IReadOnlyList<MutationAbilityIconViewData> Abilities { get; }

        public MutationCardFaceViewData(
            string partName,
            Sprite partIcon,
            IReadOnlyList<MutationAbilityIconViewData> abilities)
        {
            PartName = partName;
            PartIcon = partIcon;
            Abilities = abilities ?? Array.Empty<MutationAbilityIconViewData>();
        }
    }
}
