using System.Linq;
using Combat.Core.StatusEffects;

namespace Combat.Core
{
    /// <summary>
    /// Round bookkeeping shared by the PvE and Arena round loops: status-effect TurnStart/TurnEnd
    /// triggers, duration ticking, cooldown decrement, and the acted-flag reset — once per round
    /// for ALL units. The round-end pass is the ONE deterministic status resolve point
    /// (combat-status-effects FR3): a DoT/HoT ticks, then durations count down, then expired
    /// effects drop — identically in PvE and Arena.
    /// </summary>
    public class RoundLifecycleProcessor
    {
        private readonly StatusEffectTriggerProcessor _triggerProcessor;

        public RoundLifecycleProcessor(StatusEffectTriggerProcessor triggerProcessor)
        {
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
