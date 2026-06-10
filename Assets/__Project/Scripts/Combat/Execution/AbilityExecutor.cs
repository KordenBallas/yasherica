using Combat.Core;
using Combat.Core.StatusEffects;
using System.Linq;

namespace Combat.Execution
{
    /// <summary>
    /// Executes a scheduled ability against all units in its affected area.
    /// Affected cells are recomputed from the caster's current position at execution time.
    /// </summary>
    public class AbilityExecutor : IAbilityExecutor
    {
        private readonly IDamageSystem _damageSystem;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;
        private readonly IAbilityShapeCalculator _shapeCalculator;

        public AbilityExecutor(
            IDamageSystem damageSystem,
            StatusEffectTriggerProcessor triggerProcessor,
            IAbilityShapeCalculator shapeCalculator)
        {
            _damageSystem = damageSystem;
            _triggerProcessor = triggerProcessor;
            _shapeCalculator = shapeCalculator;
        }

        public ICombatState ExecuteAbility(ICombatState gameState, IUnit caster, ScheduledAbility scheduledAbility)
        {
            var ability = scheduledAbility.Ability.Ability;
            var target = scheduledAbility.Target;

            var affectedCells = _shapeCalculator.GetAffectedCells(
                ability.Shape,
                caster.Position,
                target.Direction,
                gameState.IsPositionValid);

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
                    newState = _damageSystem.ApplyDamage(newState, unitAtCell, ((IDamageAbility)ability).Damage);

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
