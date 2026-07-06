using Combat.Battlefield;
using Combat.Core;
using UnityEngine;

namespace Combat.Integration
{
    /// <summary>
    /// The one placement rule every unit spawns through (D6): the closest free in-boundary cell
    /// to the unit's own world position. "Free" reads the live combat state, so units integrated
    /// sequentially (hero first, then each enemy) can never share a cell — the one-unit-per-cell
    /// invariant holds from the first Plan phase. Deterministic: distance ties break by (Q, R).
    /// </summary>
    public static class SpawnCellResolver
    {
        public static HexCoordinates Resolve(Vector3 worldPosition, IBattlefield battlefield, ICombatState state)
        {
            var direct = battlefield.WorldToHex(worldPosition);
            if (battlefield.IsCellInBoundary(direct) && IsFree(direct, state))
                return direct;

            var cells = battlefield.GetCellsInBoundary();
            HexCoordinates best = default;
            bool found = false;
            float bestDistance = float.MaxValue;

            foreach (var cell in cells)
            {
                if (!IsFree(cell, state))
                    continue;

                float distance = Vector3.Distance(worldPosition, battlefield.HexToWorld(cell));
                if (!found
                    || distance < bestDistance
                    || (Mathf.Approximately(distance, bestDistance) && IsBeforeInGridOrder(cell, best)))
                {
                    bestDistance = distance;
                    best = cell;
                    found = true;
                }
            }

            // A board smaller than its unit count is an authoring error; falling back to the
            // direct cell keeps the spawn alive instead of throwing mid-combat-setup.
            return found ? best : direct;
        }

        private static bool IsFree(HexCoordinates cell, ICombatState state)
        {
            return state?.GetUnitAt(cell) == null;
        }

        private static bool IsBeforeInGridOrder(HexCoordinates a, HexCoordinates b)
        {
            return a.Q != b.Q ? a.Q < b.Q : a.R < b.R;
        }
    }
}
