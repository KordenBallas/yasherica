using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using Core.Logging;

namespace Combat.TurnManagement
{
    /// <summary>
    /// Plan phase: asks every enemy unit's AI for its action up front and snapshots the
    /// committed intent (facing + exact cells) so it can be revealed, telegraphed, and
    /// later resolved verbatim. The AI scoring lives in the decision makers (see
    /// Combat.Player.AI) — only decide-timing (round start) and commitment (lock + reveal)
    /// live here.
    /// Deterministic given deterministic decision makers: units are planned in UnitId order.
    /// </summary>
    public class EnemyIntentPlanner
    {
        private readonly IAbilityShapeCalculator _shapeCalculator;
        private readonly IGameLogger _logger;

        public EnemyIntentPlanner(IAbilityShapeCalculator shapeCalculator, IGameLogger logger)
        {
            _shapeCalculator = shapeCalculator;
            _logger = logger;
        }

        public IReadOnlyList<EnemyIntent> Plan(ICombatState state)
        {
            var intents = new List<EnemyIntent>();

            var enemyUnits = state.Units
                .Where(u => u.IsAlive && u.Owner is AIPlayer)
                .OrderBy(u => u.Id)
                .ToList();

            foreach (var unit in enemyUnits)
            {
                var ai = (AIPlayer)unit.Owner;
                var action = ai.RequestAction(state, unit);
                if (action == null)
                {
                    _logger.Warning(LogCategory.Combat,
                        $"[EnemyIntentPlanner] AI returned null action for unit {unit.Id}; skipping");
                    continue;
                }

                intents.Add(BuildIntent(state, unit, action));
            }

            _logger.Info(LogCategory.Combat,
                $"[EnemyIntentPlanner] Planned {intents.Count} enemy intent(s) for the round");
            return intents;
        }

        private EnemyIntent BuildIntent(ICombatState state, IUnit unit, IAction action)
        {
            if (action is ScheduleAbilityAction schedule)
            {
                var abilityInstance = unit.GetAbility(schedule.AbilityId);
                if (abilityInstance != null)
                {
                    var shape = abilityInstance.Ability.Shape;
                    var facing = shape.Type == AbilityShapeType.Line
                        ? (schedule.FacingToSet ?? unit.FacingDirection)
                        : (HexDirection?)null;

                    var cells = _shapeCalculator.GetAffectedCells(
                        shape, unit.Position, facing, state.IsPositionValid);

                    return new EnemyIntent(unit.Id, action, facing, unit.Position, cells);
                }

                _logger.Warning(LogCategory.Combat,
                    $"[EnemyIntentPlanner] Unit {unit.Id} committed unknown ability {schedule.AbilityId}");
            }

            return new EnemyIntent(unit.Id, action, null, unit.Position, null);
        }
    }
}
