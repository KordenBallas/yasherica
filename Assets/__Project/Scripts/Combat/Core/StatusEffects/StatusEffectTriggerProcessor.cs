using Combat.Data.Definitions;
using Combat.Execution;

namespace Combat.Core.StatusEffects
{
    /// <summary>
    /// Processes status effect triggers during combat.
    /// Pure C# service - injected into CombatController and AbilityExecutor.
    /// </summary>
    public class StatusEffectTriggerProcessor
    {
        private readonly IDamageSystem _damageSystem;

        public StatusEffectTriggerProcessor(IDamageSystem damageSystem)
        {
            _damageSystem = damageSystem;
        }

        /// <summary>
        /// Processes all effects with the specified trigger type for a unit.
        /// </summary>
        /// <param name="state">Current combat state.</param>
        /// <param name="unit">The unit whose effects to process.</param>
        /// <param name="triggerType">The trigger type to process.</param>
        /// <returns>Updated combat state after applying triggered effects.</returns>
        public ICombatState ProcessTrigger(
            ICombatState state,
            IUnit unit,
            StatusEffectTriggerType triggerType)
        {
            var newState = state;

            foreach (var effect in unit.StatusEffects)
            {
                if (effect is ITriggeredStatusEffect triggered &&
                    triggered.TriggerType.HasFlag(triggerType))
                {
                    newState = ApplyTriggeredEffect(newState, unit.Id, triggered);

                    // Re-fetch unit in case HP changed
                    unit = newState.GetUnit(unit.Id);
                    if (unit == null || !unit.IsAlive)
                        break;
                }
            }

            return newState;
        }

        /// <summary>
        /// Processes threshold-based effects when HP changes.
        /// Checks if HP crossed a threshold and triggers appropriate effects.
        /// </summary>
        /// <param name="state">Current combat state.</param>
        /// <param name="unit">The unit whose HP changed.</param>
        /// <param name="previousHp">HP before the change.</param>
        /// <param name="currentHp">HP after the change.</param>
        /// <returns>Updated combat state after applying threshold effects.</returns>
        public ICombatState ProcessThresholdTriggers(
            ICombatState state,
            IUnit unit,
            int previousHp,
            int currentHp)
        {
            if (unit.MaxHP <= 0)
                return state;

            var newState = state;
            float previousPercent = (float)previousHp / unit.MaxHP;
            float currentPercent = (float)currentHp / unit.MaxHP;

            foreach (var effect in unit.StatusEffects)
            {
                if (effect is ITriggeredStatusEffect triggered &&
                    triggered.TriggerType.HasFlag(StatusEffectTriggerType.OnThreshold))
                {
                    bool shouldTrigger = ShouldTriggerThreshold(
                        triggered, previousPercent, currentPercent);

                    if (shouldTrigger)
                    {
                        newState = ApplyTriggeredEffect(newState, unit.Id, triggered);

                        // Re-fetch unit in case HP changed
                        unit = newState.GetUnit(unit.Id);
                        if (unit == null || !unit.IsAlive)
                            break;
                    }
                }
            }

            return newState;
        }

        private bool ShouldTriggerThreshold(
            ITriggeredStatusEffect effect,
            float previousPercent,
            float currentPercent)
        {
            float threshold = effect.HpThreshold;

            return effect.ThresholdDirection == ThresholdDirection.Below
                ? (previousPercent >= threshold && currentPercent < threshold)
                : (previousPercent <= threshold && currentPercent > threshold);
        }

        private ICombatState ApplyTriggeredEffect(
            ICombatState state,
            int unitId,
            ITriggeredStatusEffect effect)
        {
            var newState = state;
            var unit = newState.GetUnit(unitId);

            if (unit == null || !unit.IsAlive)
                return newState;

            // Apply damage if effect deals damage
            if (effect.DamagePerTrigger > 0)
            {
                newState = _damageSystem.ApplyDamage(newState, unit, effect.DamagePerTrigger);

                // Re-fetch unit after damage
                unit = newState.GetUnit(unitId);
                if (unit == null || !unit.IsAlive)
                    return newState;
            }

            // Apply healing if effect heals
            if (effect.HealPerTrigger > 0)
            {
                newState = _damageSystem.ApplyHealing(newState, unit, effect.HealPerTrigger);
            }

            return newState;
        }
    }
}
