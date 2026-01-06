using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// Interface for AI decision making strategies.
    /// </summary>
    public interface IAIDecisionMaker
    {
        /// <summary>
        /// Decides what action to take for a given unit.
        /// </summary>
        IAction DecideAction(ICombatState gameState, IUnit unit);
    }
}

