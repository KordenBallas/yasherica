using System;
using System.Collections.Generic;

namespace World.Dressing.Core
{
    /// <summary>
    /// Edge-fit math for decoration placement (decoration-footprint brief FR1/FR3): how far a
    /// point sits from the platform's true silhouette (the stitched walkable outline), and the
    /// deterministic inward nudge that makes a prop of a given footprint fit fully inside it.
    /// Pure C#; no randomness — a nudge is an exact adjustment of an already-seeded position.
    /// </summary>
    public static class PlatformEdgeFit
    {
        /// <summary>A nudge lands on a corner of a concave outline occasionally; retry a few times.</summary>
        private const int MaxNudges = 4;

        /// <summary>
        /// Distance from the point to the outline polygon: positive inside (the clearance a prop
        /// has to the edge), negative outside.
        /// </summary>
        public static float SignedClearance(IReadOnlyList<(float X, float Z)> outline, float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Count];
                float distance = DistanceToSegment(x, z, a.X, a.Z, b.X, b.Z);
                best = Math.Min(best, distance);
            }

            return IsInside(outline, x, z) ? best : -best;
        }

        /// <summary>
        /// True when the point already fits (clearance ≥ required) or could be nudged inward to a
        /// spot that does; the fitted position comes out in <paramref name="fitX"/>/<paramref name="fitZ"/>.
        /// The prop is moved by exactly what its footprint needs — density and the clustered look
        /// survive, the edge just "breathes" (brief FR3).
        /// </summary>
        public static bool TryFitInside(
            IReadOnlyList<(float X, float Z)> outline,
            float x,
            float z,
            float requiredClearance,
            out float fitX,
            out float fitZ)
        {
            fitX = x;
            fitZ = z;
            if (outline == null || outline.Count < 3)
            {
                return false;
            }

            for (int attempt = 0; ; attempt++)
            {
                if (SignedClearance(outline, fitX, fitZ) >= requiredClearance)
                {
                    return true;
                }

                if (attempt >= MaxNudges)
                {
                    return false;
                }

                ClosestPointOnOutline(outline, fitX, fitZ, out float cx, out float cz, out float nx, out float nz);

                // The outline's winding is not assumed: probe the segment normal both ways and
                // keep the side with more room (deterministic — pure geometry).
                float candidateAx = cx + nx * requiredClearance;
                float candidateAz = cz + nz * requiredClearance;
                float candidateBx = cx - nx * requiredClearance;
                float candidateBz = cz - nz * requiredClearance;
                if (SignedClearance(outline, candidateAx, candidateAz)
                    >= SignedClearance(outline, candidateBx, candidateBz))
                {
                    fitX = candidateAx;
                    fitZ = candidateAz;
                }
                else
                {
                    fitX = candidateBx;
                    fitZ = candidateBz;
                }
            }
        }

        private static void ClosestPointOnOutline(
            IReadOnlyList<(float X, float Z)> outline,
            float x,
            float z,
            out float closestX,
            out float closestZ,
            out float normalX,
            out float normalZ)
        {
            closestX = outline[0].X;
            closestZ = outline[0].Z;
            normalX = 0f;
            normalZ = 1f;
            float bestSq = float.MaxValue;
            for (int i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Count];
                ProjectOnSegment(x, z, a.X, a.Z, b.X, b.Z, out float px, out float pz);
                float dx = x - px;
                float dz = z - pz;
                float sq = dx * dx + dz * dz;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    closestX = px;
                    closestZ = pz;
                    float segX = b.X - a.X;
                    float segZ = b.Z - a.Z;
                    float length = (float)Math.Sqrt(segX * segX + segZ * segZ);
                    if (length > 1e-6f)
                    {
                        normalX = -segZ / length;
                        normalZ = segX / length;
                    }
                }
            }
        }

        private static float DistanceToSegment(
            float x, float z, float ax, float az, float bx, float bz)
        {
            ProjectOnSegment(x, z, ax, az, bx, bz, out float px, out float pz);
            float dx = x - px;
            float dz = z - pz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static void ProjectOnSegment(
            float x, float z, float ax, float az, float bx, float bz,
            out float px, out float pz)
        {
            float segX = bx - ax;
            float segZ = bz - az;
            float lengthSq = segX * segX + segZ * segZ;
            if (lengthSq < 1e-12f)
            {
                px = ax;
                pz = az;
                return;
            }

            float t = ((x - ax) * segX + (z - az) * segZ) / lengthSq;
            t = Math.Max(0f, Math.Min(1f, t));
            px = ax + segX * t;
            pz = az + segZ * t;
        }

        private static bool IsInside(IReadOnlyList<(float X, float Z)> outline, float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            {
                var a = outline[i];
                var b = outline[j];
                if (a.Z > z != b.Z > z
                    && x < (b.X - a.X) * (z - a.Z) / (b.Z - a.Z) + a.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
