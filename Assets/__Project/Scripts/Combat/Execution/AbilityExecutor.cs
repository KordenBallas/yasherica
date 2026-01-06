using Combat.Core;
using System.Linq;

namespace Combat.Execution
{
    /// <summary>
    /// Concrete implementation of ability executor.
    /// Handles damage, healing, and status effect abilities.
    /// </summary>
    public class AbilityExecutor : IAbilityExecutor
    {
        private readonly IDamageSystem _damageSystem;
        
        public AbilityExecutor(IDamageSystem damageSystem)
        {
            _damageSystem = damageSystem;
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

