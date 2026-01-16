using Combat.Core;
using Combat.Core.StatusEffects;
using System.Linq;

namespace Combat.Execution
{
    /// <summary>
    /// Concrete implementation of ability executor.
    /// Handles damage, healing, and status effect abilities.
    /// Processes OnAttack and OnHit triggers for data-driven status effects.
    /// </summary>
    public class AbilityExecutor : IAbilityExecutor
    {
        private readonly IDamageSystem _damageSystem;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;

        public AbilityExecutor(IDamageSystem damageSystem, StatusEffectTriggerProcessor triggerProcessor)
        {
            _damageSystem = damageSystem;
            _triggerProcessor = triggerProcessor;
        }
        
        public ICombatState ExecuteAbility(ICombatState gameState, IUnit caster, ScheduledAbility scheduledAbility)
        {
            var ability = scheduledAbility.Ability.Ability;
            var target = scheduledAbility.Target;

            // Get target unit if applicable
            IUnit targetUnit = null;
            if (target.TargetUnitId.HasValue)
            {
                targetUnit = gameState.GetUnit(target.TargetUnitId.Value);
                if (targetUnit == null)
                {
                    // Target no longer exists (died), skip execution
                    return gameState;
                }
            }
            else if (target.Type == AbilityTargetType.Self)
            {
                targetUnit = caster;
            }

            // Execute based on ability type
            var newState = gameState;
            bool isDamageAbility = ability is IDamageAbility;
            bool isHealAbility = ability is IHealAbility;

            // Track HP before ability for threshold triggers
            int targetPreviousHp = targetUnit?.CurrentHP ?? 0;

            // Handle damage abilities
            if (ability is IDamageAbility damageAbility && targetUnit != null)
            {
                newState = _damageSystem.ApplyDamage(newState, targetUnit, damageAbility.Damage);
            }

            // Handle healing abilities
            if (ability is IHealAbility healAbility && targetUnit != null)
            {
                newState = _damageSystem.ApplyHealing(newState, targetUnit, healAbility.HealAmount);
            }

            // Handle status effect abilities
            if (ability is IStatusEffectAbility statusAbility && targetUnit != null)
            {
                newState = ApplyStatusEffect(newState, targetUnit, statusAbility);
            }

            // Process OnAttack triggers for caster (after executing the ability)
            if (_triggerProcessor != null && isDamageAbility)
            {
                var updatedCaster = newState.GetUnit(caster.Id);
                if (updatedCaster != null && updatedCaster.IsAlive)
                {
                    newState = _triggerProcessor.ProcessTrigger(
                        newState, updatedCaster, StatusEffectTriggerType.OnAttack);
                }
            }

            // Process OnHit triggers for target (when target receives damage)
            if (_triggerProcessor != null && isDamageAbility && targetUnit != null)
            {
                var updatedTarget = newState.GetUnit(targetUnit.Id);
                if (updatedTarget != null && updatedTarget.IsAlive)
                {
                    newState = _triggerProcessor.ProcessTrigger(
                        newState, updatedTarget, StatusEffectTriggerType.OnHit);
                }
            }

            // Process OnThreshold triggers when HP changes (from damage or healing)
            if (_triggerProcessor != null && (isDamageAbility || isHealAbility) && targetUnit != null)
            {
                var updatedTarget = newState.GetUnit(targetUnit.Id);
                if (updatedTarget != null)
                {
                    int currentHp = updatedTarget.CurrentHP;
                    if (currentHp != targetPreviousHp)
                    {
                        newState = _triggerProcessor.ProcessThresholdTriggers(
                            newState, updatedTarget, targetPreviousHp, currentHp);
                    }
                }
            }

            return newState;
        }
        
        private ICombatState ApplyStatusEffect(ICombatState gameState, IUnit target, IStatusEffectAbility ability)
        {
            var effect = ability.EffectToApply;
            var updatedTarget = target as Unit;
            
            // Check if effect already exists and is stackable
            var existingEffect = target.StatusEffects.FirstOrDefault(e => e.Id == effect.Id);
            if (existingEffect != null && existingEffect.IsStackable)
            {
                // Stack the effect
                var stackedEffect = (existingEffect as StatusEffect).AddStack();
                var newEffects = target.StatusEffects
                    .Select(e => e.Id == effect.Id ? stackedEffect : e)
                    .ToList();
                updatedTarget = updatedTarget.WithStatusEffects(newEffects);
            }
            else if (existingEffect == null)
            {
                // Add new effect
                var newEffects = target.StatusEffects.Concat(new[] { effect }).ToList();
                updatedTarget = updatedTarget.WithStatusEffects(newEffects);
            }
            // If effect exists and is not stackable, do nothing (don't refresh duration)
            
            return (gameState as CombatState).WithUpdatedUnit(updatedTarget);
        }
    }
}

