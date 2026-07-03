using System.Linq;
using Combat.Core.StatusEffects;
using Combat.Execution;

namespace Combat.Core
{
    /// <summary>
    /// Round bookkeeping shared by the PvE and Arena round loops: status-effect TurnStart/TurnEnd
    /// triggers, legacy DOT/HOT effects, duration ticking, cooldown decrement, and the acted-flag
    /// reset — once per round for ALL units. Extracted verbatim from <c>CombatController</c>
    /// (behavior-preserving) so the Arena controller ticks the identical cadence.
    /// </summary>
    public class RoundLifecycleProcessor
    {
        private readonly IDamageSystem _damageSystem;
        private readonly StatusEffectTriggerProcessor _triggerProcessor;

        public RoundLifecycleProcessor(
            IDamageSystem damageSystem,
            StatusEffectTriggerProcessor triggerProcessor)
        {
            _damageSystem = damageSystem;
            _triggerProcessor = triggerProcessor;
        }

        public ICombatState ResetUnitsForNewRound(ICombatState gameState)
        {
            var newState = gameState;

            foreach (var unit in gameState.Units)
            {
                // Reset HasActedThisTurn
                var updatedUnit = (unit as Unit).WithActedThisTurn(false);

                // Decrement ability cooldowns
                var newAbilities = updatedUnit.Abilities
                    .Select(a => (a as AbilityInstance).DecrementCooldown())
                    .ToList();
                updatedUnit = updatedUnit.WithAbilities(newAbilities);

                newState = (newState as CombatState).WithUpdatedUnit(updatedUnit);
            }

            return newState;
        }

        public ICombatState ApplyRoundStartEffects(ICombatState gameState)
        {
            var newState = gameState;

            foreach (var unit in gameState.Units)
            {
                // Use trigger processor for data-driven effects (TurnStart)
                if (_triggerProcessor != null)
                {
                    var currentUnit = newState.GetUnit(unit.Id);
                    if (currentUnit != null && currentUnit.IsAlive)
                    {
                        newState = _triggerProcessor.ProcessTrigger(
                            newState, currentUnit, StatusEffectTriggerType.TurnStart);
                    }
                }

                // Also apply legacy DOT/HOT effects for backward compatibility
                var updatedUnit = newState.GetUnit(unit.Id);
                if (updatedUnit != null && updatedUnit.IsAlive)
                {
                    newState = ApplyLegacyStatusEffects(newState, updatedUnit);
                }
            }

            return newState;
        }

        public ICombatState ApplyRoundEndEffects(ICombatState gameState)
        {
            var newState = gameState;

            foreach (var unit in gameState.Units)
            {
                // Use trigger processor for data-driven effects (TurnEnd)
                if (_triggerProcessor != null)
                {
                    var currentUnit = newState.GetUnit(unit.Id);
                    if (currentUnit != null && currentUnit.IsAlive)
                    {
                        newState = _triggerProcessor.ProcessTrigger(
                            newState, currentUnit, StatusEffectTriggerType.TurnEnd);
                    }
                }

                // Decrement status effect durations
                var updatedUnit = newState.GetUnit(unit.Id);
                if (updatedUnit != null)
                {
                    newState = DecrementStatusEffects(newState, updatedUnit);
                }
            }

            return newState;
        }

        /// <summary>
        /// Applies legacy hardcoded status effects (PoisonEffect, RegenerationEffect).
        /// Kept for backward compatibility with existing status effect implementations.
        /// </summary>
        private ICombatState ApplyLegacyStatusEffects(ICombatState gameState, IUnit unit)
        {
            var newState = gameState;

            foreach (var effect in unit.StatusEffects)
            {
                // Skip data-driven effects (they're handled by trigger processor)
                if (effect is ITriggeredStatusEffect)
                    continue;

                if (effect is PoisonEffect poison)
                {
                    newState = _damageSystem.ApplyDamage(newState, unit, poison.DamagePerTurn);
                }
                else if (effect is RegenerationEffect regen)
                {
                    newState = _damageSystem.ApplyHealing(newState, unit, regen.HealPerTurn);
                }
            }

            return newState;
        }

        private ICombatState DecrementStatusEffects(ICombatState gameState, IUnit unit)
        {
            var updatedUnit = gameState.GetUnit(unit.Id) as Unit;

            // Decrement durations and remove expired effects (infinite/negative durations persist).
            var newEffects = StatusEffectDurations.Tick(updatedUnit.StatusEffects);

            updatedUnit = updatedUnit.WithStatusEffects(newEffects);
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
    }
}
