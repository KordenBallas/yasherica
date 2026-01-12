using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Defines the contract for HexCell state behavior.
    /// States control cell appearance, interaction rules, and transitions.
    /// Follows the same pattern as IPlatformState.
    /// </summary>
    public interface IHexCellState
    {
        /// <summary>
        /// Called when this state is entered.
        /// Use for initialization and logging.
        /// </summary>
        /// <param name="cell">The cell entering this state</param>
        void OnEnter(IHexCell cell);

        /// <summary>
        /// Called every frame while this state is active.
        /// Most states won't need this, but it's available for animated states.
        /// </summary>
        /// <param name="cell">The cell in this state</param>
        void OnUpdate(IHexCell cell);

        /// <summary>
        /// Called when exiting this state.
        /// Use for cleanup and logging.
        /// </summary>
        /// <param name="cell">The cell exiting this state</param>
        void OnExit(IHexCell cell);

        /// <summary>
        /// Validates whether transition to target state is allowed.
        /// Enforces state machine transition rules.
        /// </summary>
        /// <param name="targetState">The state attempting to transition to</param>
        /// <returns>True if transition is allowed, false otherwise</returns>
        bool CanTransitionTo(IHexCellState targetState);

        /// <summary>
        /// Gets the visual color for this state.
        /// Used by the view layer to render the cell.
        /// </summary>
        /// <returns>Color for this state</returns>
        Color GetColor();

        /// <summary>
        /// Gets the state type for query purposes.
        /// Allows checking what kind of state this is without type casting.
        /// </summary>
        HexCellStateType StateType { get; }
    }
}
