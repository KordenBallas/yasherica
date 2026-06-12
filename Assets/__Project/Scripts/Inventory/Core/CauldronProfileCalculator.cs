using System;

namespace Inventory.Core
{
    /// <summary>
    /// Builds the 2D lathe profile of a witch's cauldron: a flat-bottomed sphere
    /// slice (narrow base, wide belly, slightly tucked-in mouth) with an outward
    /// rim lip. Outer and inner polylines run bottom-to-rim and are index-paired
    /// so cut-plane caps reduce to quad strips.
    /// </summary>
    public static class CauldronProfileCalculator
    {
        // The belly silhouette is a sphere slice: sweeping the polar angle between
        // these bounds gives the classic pot shape. Below 90 deg the wall widens,
        // above it the mouth tucks back in.
        private const float BottomAngleRadians = 0.698132f; // 40 degrees
        private const float TopAngleRadians = 2.181662f;    // 125 degrees

        // Keeps lathe vertices off the exact rotation axis so no triangle degenerates.
        private const float AxisRadius = 0.0001f;

        public static CauldronProfile Calculate(in CauldronProfileSettings settings)
        {
            int bellyPoints = settings.WallSegments;
            int totalPoints = bellyPoints + 2; // axis point + belly curve + rim lip
            var outer = new ProfilePoint[totalPoints];
            var inner = new ProfilePoint[totalPoints];

            // Pair 0 sits on the axis; the inner point is lifted by the wall
            // thickness so the floor reads as a solid slab in the cross-section.
            outer[0] = new ProfilePoint(AxisRadius, 0f);
            inner[0] = new ProfilePoint(AxisRadius, settings.WallThickness);

            for (int i = 0; i < bellyPoints; i++)
            {
                float t = i / (float)(bellyPoints - 1);
                float angle = BottomAngleRadians + (TopAngleRadians - BottomAngleRadians) * t;
                float radius = settings.BowlRadius * (float)Math.Sin(angle);
                float height = settings.BowlDepth * t;

                outer[i + 1] = new ProfilePoint(radius, height);
                inner[i + 1] = new ProfilePoint(
                    Math.Max(AxisRadius, radius - settings.WallThickness),
                    Math.Max(height, settings.WallThickness));
            }

            // Rim lip: the outer surface flares outward at the mouth while the
            // inner surface stays put, so the rim reads as a thick rolled edge.
            ProfilePoint outerMouth = outer[totalPoints - 2];
            ProfilePoint innerMouth = inner[totalPoints - 2];
            outer[totalPoints - 1] = new ProfilePoint(outerMouth.Radius + settings.RimWidth, settings.BowlDepth);
            inner[totalPoints - 1] = new ProfilePoint(innerMouth.Radius, settings.BowlDepth);

            return new CauldronProfile(outer, inner);
        }
    }
}
