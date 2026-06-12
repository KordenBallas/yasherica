using Inventory.Core;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// View-layer DTO for one bubble inside the pot, so views never receive
    /// domain objects directly.
    /// </summary>
    public readonly struct BubbleViewData
    {
        public int InstanceId { get; }
        public Sprite Icon { get; }
        public Color Tint { get; }
        public BubblePlacement Placement { get; }

        public BubbleViewData(int instanceId, Sprite icon, Color tint, BubblePlacement placement)
        {
            InstanceId = instanceId;
            Icon = icon;
            Tint = tint;
            Placement = placement;
        }
    }
}
