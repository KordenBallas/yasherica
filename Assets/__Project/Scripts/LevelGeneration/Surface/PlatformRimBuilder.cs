using System;
using System.Collections.Generic;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// Builds the decorative organic rim ring around a platform outline: the outline is subdivided
    /// with edge midpoints (for silhouette resolution), and every vertex is pushed outward by a
    /// jittered rim width plus a small tangential wobble. The rim is dressing only — it never enters
    /// the walkable outline or the colliders. Deterministic per <see cref="Narrative.Director.Core.IRandomSource"/>.
    /// </summary>
    public static class PlatformRimBuilder
    {
        /// <summary>Tangential wobble amplitude as a fraction of the local segment length.</summary>
        private const float TangentialWobbleFraction = 0.25f;

        private const float PercentToFraction = 1f / 100f;

        public static (IReadOnlyList<(float X, float Z)> SubdividedOutline, IReadOnlyList<(float X, float Z)> RimRing) Build(
            IReadOnlyList<(float X, float Z)> outline,
            float rimWidth,
            int rimJitterPercent,
            Narrative.Director.Core.IRandomSource rng)
        {
            if (outline == null || outline.Count < 3)
            {
                throw new ArgumentException("Rim needs a closed outline of at least 3 vertices.", nameof(outline));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            // Subdivide: original vertex, then the midpoint toward the next vertex.
            var subdivided = new List<(float X, float Z)>(outline.Count * 2);
            for (int i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Count];
                subdivided.Add(a);
                subdivided.Add(((a.X + b.X) * 0.5f, (a.Z + b.Z) * 0.5f));
            }

            var rim = new List<(float X, float Z)>(subdivided.Count);
            for (int i = 0; i < subdivided.Count; i++)
            {
                var prev = subdivided[(i - 1 + subdivided.Count) % subdivided.Count];
                var current = subdivided[i];
                var next = subdivided[(i + 1) % subdivided.Count];

                // Outward = right side of the counter-clockwise winding (interior on the left).
                var normal = OutwardNormal(prev, current, next);
                var tangent = Normalize(next.X - prev.X, next.Z - prev.Z);
                float halfSegment = Distance(current, next);

                // One integer draw in [-pct, +pct] per component keeps the jitter replay-exact.
                float widthJitter = DrawSignedPercent(rng, rimJitterPercent);
                float offset = rimWidth * (1f + widthJitter);
                float wobble = DrawSignedPercent(rng, rimJitterPercent) * TangentialWobbleFraction * halfSegment;

                rim.Add((
                    current.X + normal.X * offset + tangent.X * wobble,
                    current.Z + normal.Z * offset + tangent.Z * wobble));
            }

            return (subdivided, rim);
        }

        private static float DrawSignedPercent(Narrative.Director.Core.IRandomSource rng, int percent)
        {
            if (percent <= 0)
            {
                return 0f;
            }

            return (rng.NextInt(2 * percent + 1) - percent) * PercentToFraction;
        }

        private static (float X, float Z) OutwardNormal(
            (float X, float Z) prev, (float X, float Z) current, (float X, float Z) next)
        {
            // Average of the two adjacent edge normals; each edge normal is the edge direction
            // rotated -90° (right side of CCW travel = outward).
            var inDir = Normalize(current.X - prev.X, current.Z - prev.Z);
            var outDir = Normalize(next.X - current.X, next.Z - current.Z);
            var normal = Normalize((inDir.Z + outDir.Z) * 0.5f, -(inDir.X + outDir.X) * 0.5f);

            // Degenerate (a 180° hairpin cannot occur on a hex outline, but stay safe): fall back to
            // the incoming edge normal.
            if (normal.X == 0f && normal.Z == 0f)
            {
                normal = (inDir.Z, -inDir.X);
            }

            return normal;
        }

        private static (float X, float Z) Normalize(float x, float z)
        {
            float length = (float)Math.Sqrt(x * x + z * z);
            if (length < 1e-9f)
            {
                return (0f, 0f);
            }

            return (x / length, z / length);
        }

        private static float Distance((float X, float Z) a, (float X, float Z) b)
        {
            float dx = b.X - a.X;
            float dz = b.Z - a.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
