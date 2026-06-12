using System;

namespace Inventory.Core
{
    /// <summary>
    /// Size tunables for the cauldron lathe profile, kept as a plain struct so the
    /// profile calculator stays UnityEngine-free. Values are mapped from the view
    /// that authors the cauldron geometry.
    /// </summary>
    public readonly struct CauldronProfileSettings
    {
        /// <summary>Radius at the widest point of the belly.</summary>
        public float BowlRadius { get; }

        /// <summary>Rim height above the cauldron bottom.</summary>
        public float BowlDepth { get; }

        /// <summary>Offset between the outer and inner wall surfaces.</summary>
        public float WallThickness { get; }

        /// <summary>How far the rim lip flares outward past the wall.</summary>
        public float RimWidth { get; }

        /// <summary>Points along the belly curve; low values keep the silhouette low-poly.</summary>
        public int WallSegments { get; }

        public CauldronProfileSettings(
            float bowlRadius,
            float bowlDepth,
            float wallThickness,
            float rimWidth,
            int wallSegments)
        {
            if (bowlRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bowlRadius), "Bowl radius must be positive.");
            }

            if (bowlDepth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bowlDepth), "Bowl depth must be positive.");
            }

            if (wallThickness <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(wallThickness), "Wall thickness must be positive.");
            }

            // The narrowest part of the belly must stay wider than the wall,
            // otherwise the inner surface would collapse through the axis.
            if (wallThickness >= bowlRadius * 0.5f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wallThickness), "Wall thickness must be well below the bowl radius.");
            }

            if (rimWidth < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rimWidth), "Rim width must not be negative.");
            }

            if (wallSegments < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(wallSegments), "At least 3 wall segments are required.");
            }

            BowlRadius = bowlRadius;
            BowlDepth = bowlDepth;
            WallThickness = wallThickness;
            RimWidth = rimWidth;
            WallSegments = wallSegments;
        }
    }
}
