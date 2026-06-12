using System;

namespace Inventory.Core
{
    /// <summary>
    /// Deterministic bubble packing inside the pot's elliptical interior using a
    /// phyllotaxis (sunflower) spiral: bubbles spread evenly from the center, and as
    /// the item count grows they get smaller and sit closer together. A second pass
    /// shrinks the shared radius until no two bubbles overlap on the screen plane.
    /// </summary>
    public class BubbleLayoutCalculator
    {
        // Golden angle (radians) gives the even, organic distribution of a sunflower head.
        private const float GoldenAngle = 2.39996323f;

        // Plastic-number fraction spreads depth offsets evenly and stays
        // decorrelated from the golden-angle X/Y spiral.
        private const float PlasticRatio = 0.7548776662f;

        // Extra XY clearance between bubbles: the stage camera is perspective, so a
        // nearer bubble projects slightly larger than its XY footprint suggests.
        private const float ProjectionSafetyMargin = 1.15f;

        public BubblePlacement[] Calculate(
            int count,
            float potHalfWidth,
            float potHalfHeight,
            float potHalfDepth,
            BubbleLayoutSettings settings)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Bubble count must not be negative.");
            }

            if (count == 0)
            {
                return new BubblePlacement[0];
            }

            float radius = CalculateBubbleRadius(count, settings);
            var placements = Place(count, potHalfWidth, potHalfHeight, potHalfDepth, radius, settings);

            // Shrinking the radius widens the reach, so distances only grow on the
            // second pass and the separation bound is guaranteed (down to MinBubbleRadius).
            float minDistance = MinPairwiseDistanceXY(placements);
            float requiredDistance = 2f * radius * ProjectionSafetyMargin;
            if (minDistance < requiredDistance)
            {
                float separatedRadius = Math.Max(
                    settings.MinBubbleRadius,
                    minDistance / (2f * ProjectionSafetyMargin));
                placements = Place(count, potHalfWidth, potHalfHeight, potHalfDepth, separatedRadius, settings);
            }

            return placements;
        }

        private static BubblePlacement[] Place(
            int count,
            float potHalfWidth,
            float potHalfHeight,
            float potHalfDepth,
            float radius,
            BubbleLayoutSettings settings)
        {
            var placements = new BubblePlacement[count];
            float reachX = Math.Max(0f, potHalfWidth - radius - settings.EdgePadding);
            float reachY = Math.Max(0f, potHalfHeight - radius - settings.EdgePadding);
            float reachZ = Math.Max(0f, potHalfDepth - radius - settings.EdgePadding);

            for (int i = 0; i < count; i++)
            {
                // Sqrt spreads samples uniformly by area instead of clustering at the center.
                float normalizedDistance = (float)Math.Sqrt((i + 0.5f) / count);
                float angle = i * GoldenAngle;

                float x = normalizedDistance * (float)Math.Cos(angle) * reachX;
                float y = normalizedDistance * (float)Math.Sin(angle) * reachY;
                float z = (i * PlasticRatio % 1f * 2f - 1f) * reachZ;

                placements[i] = new BubblePlacement(x, y, z, radius, angle);
            }

            return placements;
        }

        private static float MinPairwiseDistanceXY(BubblePlacement[] placements)
        {
            float minSquared = float.MaxValue;
            for (int i = 0; i < placements.Length; i++)
            {
                for (int j = i + 1; j < placements.Length; j++)
                {
                    float dx = placements[i].X - placements[j].X;
                    float dy = placements[i].Y - placements[j].Y;
                    float squared = dx * dx + dy * dy;
                    if (squared < minSquared)
                    {
                        minSquared = squared;
                    }
                }
            }

            return minSquared == float.MaxValue ? float.MaxValue : (float)Math.Sqrt(minSquared);
        }

        private static float CalculateBubbleRadius(int count, BubbleLayoutSettings settings)
        {
            float shrink = 1f + settings.RadiusFalloff * (float)Math.Sqrt(count);
            float radius = settings.MaxBubbleRadius / shrink;
            return Math.Min(settings.MaxBubbleRadius, Math.Max(settings.MinBubbleRadius, radius));
        }
    }
}
