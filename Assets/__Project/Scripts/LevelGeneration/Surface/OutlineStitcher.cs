using System.Collections.Generic;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// Result of sewing a hex-union outline: the stitched walkable boundary plus the flat fill
    /// triangles that pave the sewn notches, so the floor mesh continues across them and the rim
    /// starts at the stitched edge. Fill triangles are wound clockwise in XZ (Unity up-facing).
    /// </summary>
    public sealed class OutlineStitchResult
    {
        public OutlineStitchResult(
            List<(float X, float Z)> outline,
            List<((float X, float Z) A, (float X, float Z) B, (float X, float Z) C)> notchFills)
        {
            Outline = outline;
            NotchFills = notchFills;
        }

        /// <summary>The sewn boundary polygon — walls, boundary tests, and the rim follow this.</summary>
        public List<(float X, float Z)> Outline { get; }

        /// <summary>Flat floor patches paving the sewn notches (same local space as the outline).</summary>
        public List<((float X, float Z) A, (float X, float Z) B, (float X, float Z) C)> NotchFills { get; }
    }

    /// <summary>
    /// Sews the shallow V-notches a hex-union outline has wherever two boundary cells meet, so the
    /// wall colliders (and every boundary test that follows <c>TopBoundary</c>) run along a smooth
    /// stitched edge instead of catching the character on every hex corner, and returns the fill
    /// triangles that keep the floor continuous under the sewn spans. Only notches at most
    /// <see cref="MaxNotchDepthInHexSizes"/> of a hex deep are stitched — deeper concavities (bays
    /// of missing cells) keep their shape so no walkable cell ever ends up behind a wall.
    /// Pure and deterministic; winding-agnostic (orientation is derived from the signed area).
    /// </summary>
    public static class OutlineStitcher
    {
        // A single between-cells notch is half a hex edge deep (hex edge length == hex size);
        // 0.75 covers it with tolerance while leaving one-cell bays (depth >= 1.5 sizes) intact.
        private const float MaxNotchDepthInHexSizes = 0.75f;
        private const float CrossEpsilon = 1e-4f;

        public static OutlineStitchResult Stitch(IReadOnlyList<(float X, float Z)> outline, float hexSize)
        {
            var kept = new List<(float X, float Z)>();
            var fills = new List<((float X, float Z) A, (float X, float Z) B, (float X, float Z) C)>();
            if (outline == null)
            {
                return new OutlineStitchResult(kept, fills);
            }

            if (outline.Count < 4)
            {
                kept.AddRange(outline);
                return new OutlineStitchResult(kept, fills);
            }

            float orientation = SignedArea(outline) >= 0f ? 1f : -1f;
            float maxDepth = hexSize * MaxNotchDepthInHexSizes;

            var sewn = new bool[outline.Count];
            for (int i = 0; i < outline.Count; i++)
            {
                var prev = outline[(i - 1 + outline.Count) % outline.Count];
                var current = outline[i];
                var next = outline[(i + 1) % outline.Count];

                float cross = (current.X - prev.X) * (next.Z - current.Z)
                            - (current.Z - prev.Z) * (next.X - current.X);
                bool reflex = cross * orientation < -CrossEpsilon;
                sewn[i] = reflex && DistanceToChord(current, prev, next) <= maxDepth;

                if (!sewn[i])
                {
                    kept.Add(current);
                }
            }

            if (kept.Count < 3)
            {
                kept.Clear();
                kept.AddRange(outline);
                return new OutlineStitchResult(kept, fills);
            }

            AddNotchFills(outline, sewn, fills);
            return new OutlineStitchResult(kept, fills);
        }

        /// <summary>
        /// Paves every maximal run of sewn vertices: the notch polygon (kept, sewn..., kept) is
        /// fanned from the kept vertex before the run. Fans are emitted clockwise in XZ (up-facing).
        /// </summary>
        private static void AddNotchFills(
            IReadOnlyList<(float X, float Z)> outline,
            bool[] sewn,
            List<((float X, float Z) A, (float X, float Z) B, (float X, float Z) C)> fills)
        {
            int count = outline.Count;
            for (int i = 0; i < count; i++)
            {
                if (!sewn[i] || sewn[(i - 1 + count) % count])
                {
                    continue; // Not a run start.
                }

                var anchor = outline[(i - 1 + count) % count];
                int j = i;
                while (sewn[(j + 1) % count])
                {
                    j = (j + 1) % count;
                }

                for (int k = i; ; k = (k + 1) % count)
                {
                    var b = outline[k];
                    var c = outline[(k + 1) % count];
                    fills.Add(ClockwiseInXz(anchor, b, c));
                    if (k == j)
                    {
                        break;
                    }
                }
            }
        }

        private static ((float X, float Z) A, (float X, float Z) B, (float X, float Z) C) ClockwiseInXz(
            (float X, float Z) a, (float X, float Z) b, (float X, float Z) c)
        {
            float doubleArea = (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
            return doubleArea > 0f ? (a, c, b) : (a, b, c);
        }

        private static float SignedArea(IReadOnlyList<(float X, float Z)> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.X * b.Z - b.X * a.Z;
            }

            return area * 0.5f;
        }

        private static float DistanceToChord((float X, float Z) point, (float X, float Z) a, (float X, float Z) b)
        {
            float abX = b.X - a.X;
            float abZ = b.Z - a.Z;
            float lengthSq = abX * abX + abZ * abZ;
            if (lengthSq < CrossEpsilon)
            {
                float dx = point.X - a.X;
                float dz = point.Z - a.Z;
                return (float)System.Math.Sqrt(dx * dx + dz * dz);
            }

            float cross = abX * (point.Z - a.Z) - abZ * (point.X - a.X);
            return (float)(System.Math.Abs(cross) / System.Math.Sqrt(lengthSq));
        }
    }
}
