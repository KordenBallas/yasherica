using System.Linq;
using Combat.Core;
using Core.Logging;

namespace Combat.Execution
{
    /// <summary>
    /// Resolve phase: fires a committed enemy intent exactly as revealed. Locked semantics —
    /// the ability strikes the committed cells (whiffing if nothing stands there anymore),
    /// a committed move fizzles if its destination became invalid, and nothing re-targets.
    /// </summary>
    public class EnemyIntentResolver
    {
        private readonly IAbilityExecutor _abilityExecutor;
        private readonly IGameLogger _logger;

        public EnemyIntentResolver(IAbilityExecutor abilityExecutor, IGameLogger logger)
        {
            _abilityExecutor = abilityExecutor;
            _logger = logger;
        }

        public ICombatState Resolve(ICombatState state, EnemyIntent intent)
        {
            var caster = state.GetUnit(intent.UnitId);
            if (caster == null || !caster.IsAlive)
            {
                _logger.Info(LogCategory.Combat,
                    $"[EnemyIntentResolver] Unit {intent.UnitId} is gone — intent skipped");
                return state;
            }

            if (caster.ActionState == UnitActionState.Stunned)
            {
                _logger.Info(LogCategory.Combat,
                    $"[EnemyIntentResolver] Unit {intent.UnitId} is stunned — intent skipped");
                return state;
            }

            switch (intent.Action)
            {
                case ScheduleAbilityAction schedule:
                    return ResolveAbility(state, caster, intent, schedule);
                case MoveAction move:
                    return ResolveMove(state, caster, move);
                default:
                    return state;
            }
        }

        private ICombatState ResolveAbility(
            ICombatState state, IUnit caster, EnemyIntent intent, ScheduleAbilityAction schedule)
        {
            var abilityInstance = caster.GetAbility(schedule.AbilityId);
            if (abilityInstance == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[EnemyIntentResolver] Unit {caster.Id} no longer has ability {schedule.AbilityId} — fizzle");
                return state;
            }

            // Face the committed direction so the model legibly points where the blow lands.
            var actingCaster = caster as Unit;
            if (intent.CommittedFacing.HasValue && caster.FacingDirection != intent.CommittedFacing.Value)
            {
                actingCaster = actingCaster.WithFacingDirection(intent.CommittedFacing.Value);
                state = (state as CombatState).WithUpdatedUnit(actingCaster);
            }

            // Fire at the committed cells — never recomputed, never re-targeted. If the player
            // dodged, the per-cell loop simply finds nobody: the whiff is the payoff.
            state = _abilityExecutor.ExecuteAbilityAtCells(
                state, actingCaster, abilityInstance.Ability, intent.CommittedCells, intent.CommittedFacing);

            return StartCooldown(state, caster.Id, schedule.AbilityId);
        }

        private ICombatState ResolveMove(ICombatState state, IUnit caster, MoveAction move)
        {
            if (!state.IsPositionValid(move.TargetPosition) || state.GetUnitAt(move.TargetPosition) != null)
            {
                _logger.Info(LogCategory.Combat,
                    $"[EnemyIntentResolver] Unit {caster.Id} committed move to occupied/invalid " +
                    $"{move.TargetPosition.Q},{move.TargetPosition.R} — fizzle");
                return state;
            }

            // A root/slow status can land AFTER the intent was committed (during the player's
            // Act) — the committed move fizzles if it now exceeds the gated range.
            int distance = state.CalculateDistance(caster.Position, move.TargetPosition);
            if (distance > MovementRange.EffectiveFor(caster))
            {
                _logger.Info(LogCategory.Combat,
                    $"[EnemyIntentResolver] Unit {caster.Id} committed a {distance}-cell move but is " +
                    $"rooted/slowed to {MovementRange.EffectiveFor(caster)} — fizzle");
                return state;
            }

            var moved = (caster as Unit).WithPosition(move.TargetPosition);
            return (state as CombatState).WithUpdatedUnit(moved);
        }

        private static ICombatState StartCooldown(ICombatState state, int unitId, int abilityId)
        {
            var unit = state.GetUnit(unitId) as Unit;
            if (unit == null)
                return state;

            var newAbilities = unit.Abilities
                .Select(a => a.Ability.Id == abilityId ? (a as AbilityInstance).ResetCooldown() : a)
                .ToList();

            return (state as CombatState).WithUpdatedUnit(unit.WithAbilities(newAbilities));
        }
    }
}
