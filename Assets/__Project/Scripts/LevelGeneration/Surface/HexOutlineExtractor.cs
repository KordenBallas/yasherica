using System;
using System.Collections.Generic;
using Combat.Battlefield;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// Extracts the boundary polygon of a hex-cell union: every cell edge with no neighbor in the set
    /// becomes a directed border segment (counter-clockwise, interior on the left), and the segments
    /// are stitched into one closed loop. In a hex tiling every outline vertex has at most one
    /// outgoing border segment (the three cells around a corner are pairwise adjacent, so corner-only
    /// pinches cannot occur), so an edge-connected, hole-free cell set yields exactly one loop.
    /// Pure C#; deterministic for a given cell set.
    /// </summary>
    public static class HexOutlineExtractor
    {
        /// <summary>
        /// Endpoint quantization for stitching: corners of adjacent cells are computed from different
        /// centers, so their float coordinates differ in the last bits and must be keyed coarsely.
        /// </summary>
        private const float StitchEpsilon = 1e-4f;

        public static IReadOnlyList<(float X, float Z)> Extract(
            IReadOnlyCollection<HexCoordinates> cells,
            HexOrientation orientation,
            float hexSize,
            (float X, float Z) centerOffset)
        {
            if (cells == null || cells.Count == 0)
            {
                throw new ArgumentException("Cannot extract an outline from an empty cell set.", nameof(cells));
            }

            var cellSet = cells as ISet<HexCoordinates> ?? new HashSet<HexCoordinates>(cells);

            // Directed border segments keyed by quantized start point (one outgoing segment per vertex).
            var segmentsByStart = new Dictionary<(long, long), ((float X, float Z) Start, (float X, float Z) End)>();
            (long, long) firstKey = default;
            bool hasFirst = false;

            foreach (var cell in cells)
            {
                var (cx, cz) = HexMetrics.CellCenter(cell, orientation, hexSize);
                cx -= centerOffset.X;
                cz -= centerOffset.Z;

                for (int edge = 0; edge < HexMetrics.CornerCount; edge++)
                {
                    if (cellSet.Contains(cell + HexMetrics.NeighborOffsets[edge]))
                    {
                        continue;
                    }

                    var (ax, az) = HexMetrics.Corner(edge, orientation, hexSize);
                    var (bx, bz) = HexMetrics.Corner((edge + 1) % HexMetrics.CornerCount, orientation, hexSize);
                    var start = (cx + ax, cz + az);
                    var end = (cx + bx, cz + bz);

                    var key = Quantize(start);
                    segmentsByStart[key] = (start, end);

                    if (!hasFirst || IsLess(key, firstKey))
                    {
                        firstKey = key;
                        hasFirst = true;
                    }
                }
            }

            // Walk the loop from the lexicographically smallest start point (determinism).
            var loop = new List<(float X, float Z)>(segmentsByStart.Count);
            var currentKey = firstKey;
            for (int i = 0; i < segmentsByStart.Count; i++)
            {
                if (!segmentsByStart.TryGetValue(currentKey, out var segment))
                {
                    throw new InvalidOperationException(
                        "Hex outline did not close into a single loop — the cell set is not edge-connected and hole-free.");
                }

                loop.Add(segment.Start);
                currentKey = Quantize(segment.End);
            }

            if (!currentKey.Equals(firstKey))
            {
                throw new InvalidOperationException(
                    "Hex outline walk consumed all segments without returning to its start.");
            }

            // No collinear collapse needed: consecutive boundary edges of a hex union always turn by
            // ±60° (three tiling edges meet at 120° in every vertex), so straight runs cannot occur.
            return loop;
        }

        private static (long, long) Quantize((float X, float Z) point)
        {
            return ((long)Math.Round(point.X / StitchEpsilon), (long)Math.Round(point.Z / StitchEpsilon));
        }

        private static bool IsLess((long, long) a, (long, long) b)
        {
            return a.Item1 != b.Item1 ? a.Item1 < b.Item1 : a.Item2 < b.Item2;
        }
    }
}
