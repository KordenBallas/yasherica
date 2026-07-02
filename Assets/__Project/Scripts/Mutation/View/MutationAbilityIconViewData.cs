using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View-layer DTO for one ability icon on a mutation card face: the icon shown in the
    /// ability row plus the name/description its hover tooltip reads. Passives carry a
    /// marker so the view can distinguish them visually.
    /// </summary>
    public readonly struct MutationAbilityIconViewData
    {
        public string Name { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool IsPassive { get; }

        public MutationAbilityIconViewData(string name, string description, Sprite icon, bool isPassive)
        {
            Name = name;
            Description = description;
            Icon = icon;
            IsPassive = isPassive;
        }
    }
}
