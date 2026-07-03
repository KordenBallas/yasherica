using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Snapshots a unit's terminal decision into an <see cref="ArenaCommit"/> at lock time:
    /// per-ability committed cells are computed from the unit's position and final facing exactly
    /// the way <c>EnemyIntentPlanner.BuildIntent</c> does for PvE enemies, so the same resolver
    /// semantics (fire committed cells verbatim, whiff on dodge, fizzle on blocked move) apply.
    /// </summary>
    public class ArenaCommitBuilder
    {
        private readonly IAbilityShapeCalculator _shapeCalculator;

        public ArenaCommitBuilder(IAbilityShapeCalculator shapeCalculator)
        {
            _shapeCalculator = shapeCalculator;
        }

        /// <summary>
        /// Builds the commit for a human player's terminal action. Only actions with a board
        /// effect carry steps: the whole queued volley (ExecuteAbilityQueue) or a single move.
        /// ScheduleAbility (grow the queue — a local planning mutation other clients never need)
        /// and EndUnitTurn lock in with an empty commitment: the round passes, the queue stays
        /// hidden until it is executed.
        /// </summary>
        public ArenaCommit FromTerminalAction(ICombatState state, IUnit unit, IAction action)
        {
            switch (action)
            {
                case ExecuteAbilityQueueAction _:
                    return new ArenaCommit(
                        unit.Owner.Id, unit.Id, unit.FacingDirection, BuildVolleySteps(state, unit));

                case MoveAction move:
                    return new ArenaCommit(
                        unit.Owner.Id, unit.Id, unit.FacingDirection,
                        new List<EnemyIntent> { new EnemyIntent(unit.Id, move, null, unit.Position, null) });

                default:
                    // ScheduleAbility / EndUnitTurn — an empty commitment still counts as locked in.
                    return new ArenaCommit(
                        unit.Owner.Id, unit.Id, unit.FacingDirection, new List<EnemyIntent>());
            }
        }

        /// <summary>
        /// Builds the commit for an AI dummy from its single decided action (the same
        /// schedule-and-face composite shape PvE enemies emit).
        /// </summary>
        public ArenaCommit FromAiAction(ICombatState state, IUnit unit, IAction action)
        {
            if (action is ScheduleAbilityAction schedule)
            {
                var finalFacing = schedule.FacingToSet ?? unit.FacingDirection;
                var abilityInstance = unit.GetAbility(schedule.AbilityId);
                if (abilityInstance != null)
                {
                    var step = BuildAbilityIntent(state, unit, abilityInstance, finalFacing);
                    return new ArenaCommit(unit.Owner.Id, unit.Id, finalFacing, new List<EnemyIntent> { step });
                }

                return new ArenaCommit(unit.Owner.Id, unit.Id, finalFacing, new List<EnemyIntent>());
            }

            if (action is MoveAction move)
            {
                return new ArenaCommit(
                    unit.Owner.Id, unit.Id, unit.FacingDirection,
                    new List<EnemyIntent> { new EnemyIntent(unit.Id, move, null, unit.Position, null) });
            }

            return new ArenaCommit(unit.Owner.Id, unit.Id, unit.FacingDirection, new List<EnemyIntent>());
        }

        private List<EnemyIntent> BuildVolleySteps(ICombatState state, IUnit unit)
        {
            var steps = new List<EnemyIntent>();
            foreach (var scheduled in unit.AbilityQueue.OrderBy(a => a.ExecutionOrder))
            {
                steps.Add(BuildAbilityIntent(state, unit, scheduled.Ability, unit.FacingDirection));
            }

            return steps;
        }

        private EnemyIntent BuildAbilityIntent(
            ICombatState state, IUnit unit, IAbilityInstance abilityInstance, HexDirection finalFacing)
        {
            var shape = abilityInstance.Ability.Shape;
            var facing = shape.Type == AbilityShapeType.Line ? finalFacing : (HexDirection?)null;

            var cells = _shapeCalculator.GetAffectedCells(
                shape, unit.Position, facing, state.IsPositionValid);

            var action = new ScheduleAbilityAction(unit.Owner, unit.Id, abilityInstance.Ability.Id, facing);
            return new EnemyIntent(unit.Id, action, facing, unit.Position, cells);
        }
    }
}
