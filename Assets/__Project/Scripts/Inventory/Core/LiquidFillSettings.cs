using System;

namespace Inventory.Core
{
    /// <summary>
    /// Tunables for the fullness fill level (Track F): the bowl-local height
    /// band the waterline moves in and the count that reads as a full pot.
    /// Mapped from <c>InventoryConfig</c>; plain struct so the calculator stays
    /// UnityEngine-free.
    /// </summary>
    public readonly struct LiquidFillSettings
    {
        /// <summary>Waterline height of an empty pot (never bone-dry).</summary>
        public float MinHeight { get; }

        /// <summary>Waterline height of a brimming pot (never overflowing).</summary>
        public float MaxHeight { get; }

        /// <summary>Artifact count at which the pot reads as full.</summary>
        public int CountAtMax { get; }

        /// <summary>Clearance kept between the topmost bubble and the waterline.</summary>
        public float Headroom { get; }

        public LiquidFillSettings(float minHeight, float maxHeight, int countAtMax, float headroom)
        {
            if (minHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minHeight), "Min fill height must be positive.");
            }

            if (maxHeight < minHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeight), "Max fill height must not be below min.");
            }

            if (countAtMax < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(countAtMax), "Count at max must be at least 1.");
            }

            if (headroom < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(headroom), "Headroom must not be negative.");
            }

            MinHeight = minHeight;
            MaxHeight = maxHeight;
            CountAtMax = countAtMax;
            Headroom = headroom;
        }
    }
}
