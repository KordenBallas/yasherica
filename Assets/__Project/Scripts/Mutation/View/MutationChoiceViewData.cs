using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View-layer DTO for one offered mutation on the stage-up choice panel. The presenter resolves
    /// the label, icon, and archetype tint, so the view never sees Core or ScriptableObject types.
    /// </summary>
    public readonly struct MutationChoiceViewData
    {
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public Color Tint { get; }

        public MutationChoiceViewData(string displayName, Sprite icon, Color tint)
        {
            DisplayName = displayName;
            Icon = icon;
            Tint = tint;
        }
    }
}
