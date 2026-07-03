using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// An enemy unit's committed, locked plan for the round, revealed at Plan phase.
    /// Cells and facing are snapshotted at plan time so presentation (icons, ghosts)
    /// and resolution read exactly the same data — the action fires as shown.
    /// </summary>
    public class EnemyIntent
    {
        public int UnitId { get; }

        /// <summary>
        /// The decision the AI committed to (ScheduleAbilityAction, MoveAction, or EndUnitTurnAction).
        /// </summary>
        public IAction Action { get; }

        /// <summary>
        /// Facing the directional action was committed along; null for ring abilities and non-ability actions.
        /// </summary>
        public HexDirection? CommittedFacing { get; }

        /// <summary>
        /// The unit's position at plan time — committed cells were computed from here.
        /// </summary>
        public HexCoordinates CommittedOrigin { get; }

        /// <summary>
        /// The exact cells the committed ability will strike at resolve, regardless of how
        /// the board changed. Empty for moves and end-turn intents.
        /// </summary>
        public IReadOnlyList<HexCoordinates> CommittedCells { get; }

        public bool IsAbility => Action is ScheduleAbilityAction;
        public bool IsMove => Action is MoveAction;

        public int AbilityId => Action is ScheduleAbilityAction schedule ? schedule.AbilityId : -1;

        public HexCoordinates? MoveDestination =>
            Action is MoveAction move ? move.TargetPosition : (HexCoordinates?)null;

        public EnemyIntent(
            int unitId,
            IAction action,
            HexDirection? committedFacing,
            HexCoordinates committedOrigin,
            IReadOnlyList<HexCoordinates> committedCells)
        {
            UnitId = unitId;
            Action = action;
            CommittedFacing = committedFacing;
            CommittedOrigin = committedOrigin;
            CommittedCells = committedCells ?? new List<HexCoordinates>();
        }
    }
}
