using System;
using System.Collections.Generic;
using Combat.Battlefield;

namespace World.Dressing.Core
{
    /// <summary>
    /// The invariants every blocking placement must honour, shared by both planners: blocked cells
    /// keep their distance, never break the free-cell connectivity (BFS from the never-blocked
    /// center cell), and never push the free count under the battlefield minimum. Pure C#.
    /// </summary>
    public static class BlockedCellGuard
    {
        public static int HexDistance(HexCoordinates a, HexCoordinates b)
        {
            int dq = a.Q - b.Q;
            int dr = a.R - b.R;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        public static bool TooClose(HexCoordinates cell, IReadOnlyList<HexCoordinates> blocked, int minSpacing)
        {
            foreach (var other in blocked)
            {
                if (HexDistance(cell, other) < minSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the unblocked cells still form one connected field.</summary>
        public static bool StaysConnected(PlatformHexSurface surface, HashSet<HexCoordinates> blockedSet)
        {
            int freeCount = surface.Cells.Count - blockedSet.Count;
            var visited = new HashSet<HexCoordinates> { surface.CenterCell };
            var frontier = new Queue<HexCoordinates>();
            frontier.Enqueue(surface.CenterCell);
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var offset in HexMetrics.NeighborOffsets)
                {
                    var next = current + offset;
                    if (!surface.Contains(next) || blockedSet.Contains(next) || !visited.Add(next))
                    {
                        continue;
                    }

                    frontier.Enqueue(next);
                }
            }

            return visited.Count == freeCount;
        }

        /// <summary>The most cells blocking may consume while honouring the battlefield floor.</summary>
        public static int MaxBlockable(PlatformHexSurface surface, int battlefieldMinCells, int protectedCount)
        {
            return Math.Max(0, surface.Cells.Count - Math.Max(battlefieldMinCells, protectedCount));
        }
    }
}
