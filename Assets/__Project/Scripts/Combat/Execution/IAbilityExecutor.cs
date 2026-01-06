using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Executes individual abilities.
    /// </summary>
    public interface IAbilityExecutor
    {
        /// <summary>
        /// Executes a single scheduled ability.
        /// </summary>
        /// <returns>New game state after ability execution.</returns>
        ICombatState ExecuteAbility(ICombatState gameState, IUnit caster, ScheduledAbility scheduledAbility);
    }
}

