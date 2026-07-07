using System;

namespace Inventory.Core
{
    /// <summary>
    /// Tunables for the brew spot lattice (Track F stable-spot layout). Plain
    /// struct so the builder stays UnityEngine-free; values are mapped from
    /// <c>InventoryConfig</c> at install time.
    /// </summary>
    public readonly struct BrewLatticeSettings
    {
        /// <summary>Fixed radius every brew bubble uses (stable spots need stable size).</summary>
        public float BubbleRadius { get; }

        /// <summary>
        /// Extra clearance factor between spot centres (&gt;= 1). Covers the
        /// perspective camera projecting near bubbles slightly larger.
        /// </summary>
        public float SpacingMargin { get; }

        /// <summary>Clearance between a bubble and the bowl's inner wall.</summary>
        public float EdgePadding { get; }

        /// <summary>Clearance between the lowest bubbles and the bowl floor.</summary>
        public float FloorClearance { get; }

        /// <summary>Max |Z| offset per spot so the stack reads as a volume, not a plane.</summary>
        public float DepthJitter { get; }

        /// <summary>
        /// Extra spot layers above the rim, used only when the pot holds more
        /// artifacts than the submerged volume fits (overfill fallback).
        /// </summary>
        public int OverflowLayers { get; }

        public BrewLatticeSettings(
            float bubbleRadius,
            float spacingMargin,
            float edgePadding,
            float floorClearance,
            float depthJitter,
            int overflowLayers)
        {
            if (bubbleRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bubbleRadius), "Bubble radius must be positive.");
            }

            if (spacingMargin < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(spacingMargin), "Spacing margin must be at least 1.");
            }

            if (edgePadding < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(edgePadding), "Edge padding must not be negative.");
            }

            if (floorClearance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(floorClearance), "Floor clearance must not be negative.");
            }

            if (depthJitter < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(depthJitter), "Depth jitter must not be negative.");
            }

            if (overflowLayers < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(overflowLayers), "Overflow layers must not be negative.");
            }

            BubbleRadius = bubbleRadius;
            SpacingMargin = spacingMargin;
            EdgePadding = edgePadding;
            FloorClearance = floorClearance;
            DepthJitter = depthJitter;
            OverflowLayers = overflowLayers;
        }
    }
}
