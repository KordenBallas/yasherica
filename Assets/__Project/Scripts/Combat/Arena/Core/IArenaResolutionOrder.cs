using System.Collections.Generic;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Strategy seam for the round's resolution order (PO decision 2026-07-03: initiative must
    /// pass between players and stay replaceable — no player holds a permanent advantage).
    /// Implementations must be deterministic: same bundle + round number → same intent sequence
    /// on every client.
    /// </summary>
    public interface IArenaResolutionOrder
    {
        /// <summary>
        /// Flattens the bundle's commits into the exact intent sequence the round resolves,
        /// unit-by-unit; within a unit, steps keep their committed order.
        /// </summary>
        IReadOnlyList<EnemyIntent> Order(ArenaRoundBundle bundle);
    }
}
