using Combat.Core;

namespace Combat.Rules
{
    /// <summary>
    /// Base interface for game rules.
    /// </summary>
    public interface IRules
    {
        /// <summary>
        /// Validates a rule against the game state.
        /// </summary>
        bool Validate(ICombatState gameState);
    }
}

