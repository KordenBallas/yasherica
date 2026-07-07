using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Display data for one ability a body part grants, as the mutation card shows it:
    /// name + description + icon for the card face row, plus the shape/trigger fields the
    /// shared ability-preview popover needs to demonstrate the cast (a passive previews as
    /// an idle hero). Lives in the Data layer so the presenter/view never touch the Combat
    /// ability ScriptableObjects.
    /// </summary>
    public readonly struct MutationAbilityInfo
    {
        public string Name { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool IsPassive { get; }
        public bool IsLine { get; }
        public int LineLength { get; }
        public int RingRadius { get; }
        public string AnimationTrigger { get; }

        /// <summary>
        /// Glyph of the status this ability applies (FR8: the same asset shown on the unit
        /// on the board). Null = no status — the card badge falls back to the neutral
        /// untyped mark authored on the prefab.
        /// </summary>
        public Sprite StatusGlyph { get; }

        public MutationAbilityInfo(
            string name,
            string description,
            Sprite icon,
            bool isPassive,
            bool isLine = false,
            int lineLength = 0,
            int ringRadius = 0,
            string animationTrigger = "",
            Sprite statusGlyph = null)
        {
            Name = name;
            Description = description;
            Icon = icon;
            IsPassive = isPassive;
            IsLine = isLine;
            LineLength = lineLength;
            RingRadius = ringRadius;
            AnimationTrigger = animationTrigger;
            StatusGlyph = statusGlyph;
        }
    }
}
