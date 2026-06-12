namespace Inventory.Core
{
    /// <summary>
    /// Tunables for bubble placement inside the pot, kept as a plain struct so the
    /// layout calculator stays UnityEngine-free. Values are mapped from InventoryConfig.
    /// </summary>
    public readonly struct BubbleLayoutSettings
    {
        public float MinBubbleRadius { get; }
        public float MaxBubbleRadius { get; }

        /// <summary>How quickly bubbles shrink as the item count grows.</summary>
        public float RadiusFalloff { get; }

        /// <summary>Clearance kept between bubbles and the pot interior edge.</summary>
        public float EdgePadding { get; }

        public BubbleLayoutSettings(
            float minBubbleRadius,
            float maxBubbleRadius,
            float radiusFalloff,
            float edgePadding)
        {
            MinBubbleRadius = minBubbleRadius;
            MaxBubbleRadius = maxBubbleRadius;
            RadiusFalloff = radiusFalloff;
            EdgePadding = edgePadding;
        }
    }
}
