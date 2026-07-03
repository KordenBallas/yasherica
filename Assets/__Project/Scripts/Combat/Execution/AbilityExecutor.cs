using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;

namespace Combat.Execution
{
    /// <summary>
    /// Executes a scheduled ability against all units in its affected area.
    /// Affected cells are recomputed at execution time from the caster's current position
    /// and current facing — turning re-points the whole queued volley.
    /// </summary>
    public class AbilityExecutor : IAbilityExecutor
    {
        private readonly IDamageSystem _damageSystem;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;
        private readonly IAbilityShapeCalculator _shapeCalculator;
        private readonly HexDirectionConfig _hexConfig;

        public AbilityExecutor(
            IDamageSystem damageSystem,
            StatusEffectTriggerProcessor triggerProcessor,
            IAbilityShapeCalculator shapeCalculator,
            HexDirectionConfig hexConfig)
        {
            _damageSystem = damageSystem;
            _triggerProcessor = triggerProcessor;
            _shapeCalculator = shapeCalculator;
            _hexConfig = hexConfig;
        }

        public ICombatState ExecuteAbility(ICombatState gameState, IUnit caster, ScheduledAbility scheduledAbility)
        {
            var ability = scheduledAbility.Ability.Ability;

            // Read the live unit so position and facing reflect the state at execution time,
            // not the snapshot taken when the queue loop started.
            var liveCaster = gameState.GetUnit(caster.Id) ?? caster;
            var direction = ability.Shape.Type == AbilityShapeType.Line
                ? liveCaster.FacingDirection
                : (HexDirection?)null;

            var affectedCells = _shapeCalculator.GetAffectedCells(
                ability.Shape,
                liveCaster.Position,
                direction,
                gameState.IsPositionValid);

            return ExecuteAbilityAtCells(gameState, liveCaster, ability, affectedCells, direction);
        }

        public ICombatState ExecuteAbilityAtCells(
            ICombatState gameState,
            IUnit caster,
            IAbility ability,
            IReadOnlyList<HexCoordinates> cells,
            HexDirection? lineDirection)
        {
            var affectedCells = cells;
            var newState = gameState;
            bool isDamageAbility = ability is IDamageAbility;
            bool isHealAbility = ability is IHealAbility;

            foreach (var cell in affectedCells)
            {
                var unitAtCell = newState.GetUnitAt(cell);
                if (unitAtCell == null || !unitAtCell.IsAlive)
                    continue;

                int previousHp = unitAtCell.CurrentHP;

                if (isDamageAbility)
                {
                    int finalDamage = _damageSystem.CalculateFinalDamage(
                        caster, unitAtCell, ((IDamageAbility)ability).Damage);
                    newState = _damageSystem.ApplyDamage(newState, unitAtCell, finalDamage);
                }

                if (isHealAbility)
                    newState = _damageSystem.ApplyHealing(newState, unitAtCell, ((IHealAbility)ability).HealAmount);

                if (ability is IStatusEffectAbility statusAbility)
                    newState = ApplyStatusEffect(newState, unitAtCell, statusAbility);

                if (_triggerProcessor != null)
                {
                    var updatedTarget = newState.GetUnit(unitAtCell.Id);
                    if (updatedTarget != null)
                    {
                        if (isDamageAbility && updatedTarget.IsAlive)
                        {
                            newState = _triggerProcessor.ProcessTrigger(
                                newState, updatedTarget, StatusEffectTriggerType.OnHit);
                        }

                        if ((isDamageAbility || isHealAbility) && updatedTarget.CurrentHP != previousHp)
                        {
                            newState = _triggerProcessor.ProcessThresholdTriggers(
                                newState, updatedTarget, previousHp, updatedTarget.CurrentHP);
                        }
                    }
                }
            }

            // Displacement: push surviving struck units away from the caster along the line
            // direction. Processed farthest-first (reverse cell order) so pushed units never
            // collide with other affected units that are about to move too.
            if (ability is IDisplacementAbility displacementAbility
                && displacementAbility.PushDistance > 0
                && lineDirection.HasValue)
            {
                for (int i = affectedCells.Count - 1; i >= 0; i--)
                {
                    var pushedUnit = newState.GetUnitAt(affectedCells[i]);
                    if (pushedUnit == null || !pushedUnit.IsAlive || pushedUnit.Id == caster.Id)
                        continue;

                    var destination = DisplacementResolver.ResolveDestination(
                        pushedUnit.Position,
                        lineDirection.Value,
                        displacementAbility.PushDistance,
                        _hexConfig,
                        newState.IsPositionValid,
                        cell => newState.GetUnitAt(cell) != null);

                    if (!destination.Equals(pushedUnit.Position))
                    {
                        newState = (newState as CombatState)
                            .WithUpdatedUnit((pushedUnit as Unit).WithPosition(destination));
                    }
                }
            }

            // OnAttack trigger for caster fires once per ability execution
            if (_triggerProcessor != null && isDamageAbility)
            {
                var updatedCaster = newState.GetUnit(caster.Id);
                if (updatedCaster != null && updatedCaster.IsAlive)
                {
                    newState = _triggerProcessor.ProcessTrigger(
                        newState, updatedCaster, StatusEffectTriggerType.OnAttack);
                }
            }

            return newState;
        }

        private ICombatState ApplyStatusEffect(ICombatState gameState, IUnit target, IStatusEffectAbility ability)
        {
            var effect = ability.EffectToApply;
            var updatedTarget = target as Unit;

            var existingEffect = target.StatusEffects.FirstOrDefault(e => e.Id == effect.Id);
            if (existingEffect != null && existingEffect.IsStackable)
            {
                var stackedEffect = (existingEffect as StatusEffect).AddStack();
                var newEffects = target.StatusEffects
                    .Select(e => e.Id == effect.Id ? stackedEffect : e)
                    .ToList();
                updatedTarget = updatedTarget.WithStatusEffects(newEffects);
            }
            else if (existingEffect == null)
            {
                var newEffects = target.StatusEffects.Concat(new[] { effect }).ToList();
                updatedTarget = updatedTarget.WithStatusEffects(newEffects);
            }

            return (gameState as CombatState).WithUpdatedUnit(updatedTarget);
        }
    }
}
