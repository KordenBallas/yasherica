using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Narrative.Director.Core;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// Grows a platform's hex-cell set deterministically from a per-platform seeded stream: weighted
    /// blob accretion to a target cell count (compactness biases growth toward cells that hug the
    /// blob), then hole-filling so the interior is complete whole cells and the outline is a single
    /// loop (brief §3). Combat-capable platforms pass their battlefield minimum as
    /// <c>guaranteedMinCells</c> (brief §6). Pure C#; same seed → same surface.
    /// </summary>
    public sealed class PlatformSurfaceGenerator
    {
        public PlatformHexSurface Generate(
            ShapeProfile profile,
            int guaranteedMinCells,
            float hexSize,
            HexOrientation orientation,
            float rimWidth,
            int rimJitterPercent,
            IRandomSource rng)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            int effectiveMin = Math.Max(profile.MinCells, Math.Max(1, guaranteedMinCells));
            int effectiveMax = Math.Max(profile.MaxCells, effectiveMin);
            int target = effectiveMin + rng.NextInt(effectiveMax - effectiveMin + 1);

            var cells = GrowBlob(target, profile.Compactness, rng);
            FillHoles(cells);

            var centerOffset = Centroid(cells, orientation, hexSize);
            var sortedCells = new List<HexCoordinates>(cells);
            sortedCells.Sort(CompareCells);

            var outline = HexOutlineExtractor.Extract(cells, orientation, hexSize, centerOffset);
            var (subdivided, rim) = PlatformRimBuilder.Build(outline, rimWidth, rimJitterPercent, rng);

            return new PlatformHexSurface(sortedCells, orientation, hexSize, centerOffset, outline, subdivided, rim);
        }

        private static HashSet<HexCoordinates> GrowBlob(int target, int compactness, IRandomSource rng)
        {
            var cells = new HashSet<HexCoordinates> { new HexCoordinates(0, 0) };

            while (cells.Count < target)
            {
                // Candidates rebuilt each step and sorted by (Q, R): never draw over hash order —
                // the sort is what makes the growth replay-exact.
                var candidates = CollectFrontier(cells);

                int totalWeight = 0;
                var weights = new int[candidates.Count];
                for (int i = 0; i < candidates.Count; i++)
                {
                    int occupiedNeighbors = CountOccupiedNeighbors(candidates[i], cells);
                    weights[i] = 1 + (occupiedNeighbors - 1) * compactness;
                    totalWeight += weights[i];
                }

                int pick = rng.NextInt(totalWeight);
                int cumulative = 0;
                for (int i = 0; i < candidates.Count; i++)
                {
                    cumulative += weights[i];
                    if (pick < cumulative)
                    {
                        cells.Add(candidates[i]);
                        break;
                    }
                }
            }

            return cells;
        }

        private static List<HexCoordinates> CollectFrontier(HashSet<HexCoordinates> cells)
        {
            var frontier = new HashSet<HexCoordinates>();
            foreach (var cell in cells)
            {
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var neighbor = cell + offset;
                    if (!cells.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            var sorted = new List<HexCoordinates>(frontier);
            sorted.Sort(CompareCells);
            return sorted;
        }

        private static int CountOccupiedNeighbors(HexCoordinates candidate, HashSet<HexCoordinates> cells)
        {
            int count = 0;
            foreach (var offset in HexMetrics.NeighborOffsets)
            {
                if (cells.Contains(candidate + offset))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Fills enclosed holes so every interior hex is a whole walkable cell and the outline is one
        /// loop: flood-fills the complement from outside the axial bounding box (+1 margin); any
        /// non-cell hex the flood cannot reach is enclosed → added. May push the count slightly past
        /// the drawn target; deterministic.
        /// </summary>
        private static void FillHoles(HashSet<HexCoordinates> cells)
        {
            int minQ = int.MaxValue, maxQ = int.MinValue, minR = int.MaxValue, maxR = int.MinValue;
            foreach (var cell in cells)
            {
                minQ = Math.Min(minQ, cell.Q);
                maxQ = Math.Max(maxQ, cell.Q);
                minR = Math.Min(minR, cell.R);
                maxR = Math.Max(maxR, cell.R);
            }

            minQ--; maxQ++; minR--; maxR++;

            var outside = new HashSet<HexCoordinates>();
            var queue = new Queue<HexCoordinates>();
            var start = new HexCoordinates(minQ, minR);
            outside.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var next = current + offset;
                    if (next.Q < minQ || next.Q > maxQ || next.R < minR || next.R > maxR)
                    {
                        continue;
                    }

                    if (cells.Contains(next) || !outside.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            for (int q = minQ; q <= maxQ; q++)
            {
                for (int r = minR; r <= maxR; r++)
                {
                    var hex = new HexCoordinates(q, r);
                    if (!cells.Contains(hex) && !outside.Contains(hex))
                    {
                        cells.Add(hex);
                    }
                }
            }
        }

        private static (float X, float Z) Centroid(
            HashSet<HexCoordinates> cells, HexOrientation orientation, float hexSize)
        {
            float sumX = 0f, sumZ = 0f;
            foreach (var cell in cells)
            {
                var (x, z) = HexMetrics.CellCenter(cell, orientation, hexSize);
                sumX += x;
                sumZ += z;
            }

            return (sumX / cells.Count, sumZ / cells.Count);
        }

        private static int CompareCells(HexCoordinates a, HexCoordinates b)
        {
            int byQ = a.Q.CompareTo(b.Q);
            return byQ != 0 ? byQ : a.R.CompareTo(b.R);
        }
    }
}
