using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// View-layer DTO for one archetype line in the feeding readout (cumulative tray weight or a
    /// dominant archetype this stage). The presenter resolves the display name and tint from the
    /// archetype catalog, so the view never sees domain or ScriptableObject types.
    /// </summary>
    public readonly struct ArchetypeReadoutEntry
    {
        public string DisplayName { get; }
        public float Weight { get; }
        public Color Tint { get; }

        public ArchetypeReadoutEntry(string displayName, float weight, Color tint)
        {
            DisplayName = displayName;
            Weight = weight;
            Tint = tint;
        }
    }
}
