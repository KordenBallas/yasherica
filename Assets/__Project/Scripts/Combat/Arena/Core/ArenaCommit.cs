using System.Collections.Generic;
using Combat.Config;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// One player's locked round: the whole commitment (ability volley OR move OR pass) snapshotted
    /// at lock time as committed intents — cells and facing are frozen, so resolution fires them
    /// verbatim on every client (whiffs are real, nothing re-targets). The hidden-planning guarantee
    /// is structural: a commit exists only on its owner's machine until it is sent at lock.
    /// </summary>
    public class ArenaCommit
    {
        public int PlayerId { get; }
        public int UnitId { get; }

        /// <summary>
        /// The unit's facing at lock time. Applied on every client at normalization so all
        /// simulations agree on the canonical pre-resolution board.
        /// </summary>
        public HexDirection FinalFacing { get; }

        /// <summary>
        /// The committed steps in execution order: one intent per queued ability, or a single
        /// move intent, or empty for a pass.
        /// </summary>
        public IReadOnlyList<EnemyIntent> Steps { get; }

        public ArenaCommit(int playerId, int unitId, HexDirection finalFacing, IReadOnlyList<EnemyIntent> steps)
        {
            PlayerId = playerId;
            UnitId = unitId;
            FinalFacing = finalFacing;
            Steps = steps ?? new List<EnemyIntent>();
        }
    }
}
