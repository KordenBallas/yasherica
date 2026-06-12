using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// View-layer DTO for an artifact shown above the pot
    /// (staged in a crafting slot or as the crafted result).
    /// </summary>
    public readonly struct ArtifactViewData
    {
        public int InstanceId { get; }
        public Sprite Icon { get; }
        public Color Tint { get; }

        public ArtifactViewData(int instanceId, Sprite icon, Color tint)
        {
            InstanceId = instanceId;
            Icon = icon;
            Tint = tint;
        }
    }
}
