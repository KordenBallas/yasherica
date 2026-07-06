using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View-layer DTO for one ability icon on a mutation card face: the icon shown in the
    /// ability row plus everything its hover hands to the shared ability-preview popover
    /// (name/description text and the shape/trigger the 3D stage demonstrates). Passives
    /// carry a marker so the view can distinguish them visually.
    /// </summary>
    public readonly struct MutationAbilityIconViewData
    {
        public string Name { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool IsPassive { get; }
        public bool IsLine { get; }
        public int LineLength { get; }
        public int RingRadius { get; }
        public string AnimationTrigger { get; }

        public MutationAbilityIconViewData(
            string name,
            string description,
            Sprite icon,
            bool isPassive,
            bool isLine = false,
            int lineLength = 0,
            int ringRadius = 0,
            string animationTrigger = "")
        {
            Name = name;
            Description = description;
            Icon = icon;
            IsPassive = isPassive;
            IsLine = isLine;
            LineLength = lineLength;
            RingRadius = ringRadius;
            AnimationTrigger = animationTrigger;
        }
    }
}
