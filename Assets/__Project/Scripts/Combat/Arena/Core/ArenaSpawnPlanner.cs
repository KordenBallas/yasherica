using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Deterministic distinct spawn cells (brief R6): greedy farthest-point pick over the
    /// platform's cells — the first spawn is the cell farthest from the center, each next spawn
    /// maximizes its minimum distance to the already-picked set. Ties break by (Q, R), so the
    /// same surface + player count yields the same cells on every client.
    /// </summary>
    public class ArenaSpawnPlanner
    {
        public IReadOnlyList<HexCoordinates> Plan(
            IReadOnlyList<HexCoordinates> cells, HexCoordinates centerCell, int playerCount)
        {
            if (cells == null || cells.Count == 0)
                throw new ArgumentException("Cannot plan spawns on an empty surface.", nameof(cells));
            if (playerCount < 1)
                throw new ArgumentException("At least one player is required.", nameof(playerCount));
            if (playerCount > cells.Count)
                throw new ArgumentException(
                    $"Surface has {cells.Count} cells but {playerCount} spawns were requested.", nameof(playerCount));

            var candidates = cells
                .OrderByDescending(c => Distance(c, centerCell))
                .ThenBy(c => c.Q)
                .ThenBy(c => c.R)
                .ToList();

            var picked = new List<HexCoordinates> { candidates[0] };

            while (picked.Count < playerCount)
            {
                HexCoordinates best = default;
                int bestScore = -1;

                foreach (var candidate in candidates)
                {
                    if (picked.Contains(candidate))
                        continue;

                    int minDistance = picked.Min(p => Distance(candidate, p));
                    if (minDistance > bestScore)
                    {
                        bestScore = minDistance;
                        best = candidate;
                    }
                }

                picked.Add(best);
            }

            return picked;
        }

        private static int Distance(HexCoordinates from, HexCoordinates to)
        {
            var dq = Math.Abs(from.Q - to.Q);
            var dr = Math.Abs(from.R - to.R);
            var ds = Math.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }
    }
}
