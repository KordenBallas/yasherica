using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// System for applying damage and healing to units.
    /// Returns new immutable game states.
    /// </summary>
    public interface IDamageSystem
    {
        /// <summary>
        /// Applies damage to a target unit.
        /// </summary>
        /// <returns>New game state with updated unit HP.</returns>
        ICombatState ApplyDamage(ICombatState gameState, IUnit target, int amount);
        
        /// <summary>
        /// Applies healing to a target unit.
        /// </summary>
        /// <returns>New game state with updated unit HP.</returns>
        ICombatState ApplyHealing(ICombatState gameState, IUnit target, int amount);
        
        /// <summary>
        /// Calculates final damage after modifiers (for future extensions).
        /// </summary>
        int CalculateFinalDamage(IUnit attacker, IUnit target, int baseDamage);
    }
}

