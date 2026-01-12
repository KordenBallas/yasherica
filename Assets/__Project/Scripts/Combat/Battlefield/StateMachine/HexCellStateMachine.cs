using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Manages state transitions for a HexCell.
    /// Follows the same pattern as PlatformStateMachine.
    /// Validates transitions and handles state lifecycle (OnEnter/OnUpdate/OnExit).
    /// </summary>
    public class HexCellStateMachine
    {
        private IHexCellState currentState;
        private IHexCell owner;

        /// <summary>
        /// Current active state.
        /// </summary>
        public IHexCellState CurrentState => currentState;

        /// <summary>
        /// Initializes the state machine with an owner and initial state.
        /// Must be called before using the state machine.
        /// </summary>
        /// <param name="cell">The cell that owns this state machine</param>
        /// <param name="initialState">The starting state</param>
        public void Initialize(IHexCell cell, IHexCellState initialState)
        {
            owner = cell;
            ChangeState(initialState);
        }

        /// <summary>
        /// Attempts to transition to a new state.
        /// Validates transition rules and logs warnings for invalid transitions.
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        public void ChangeState(IHexCellState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning($"[HexCellStateMachine] Cell {owner?.Coordinates}: Attempted to change to null state");
                return;
            }

            // Validate transition
            if (currentState != null && !currentState.CanTransitionTo(newState))
            {
                Debug.LogWarning(
                    $"[HexCellStateMachine] Cell {owner?.Coordinates}: " +
                    $"Cannot transition from {currentState.GetType().Name} to {newState.GetType().Name}"
                );
                return;
            }

            string oldStateName = currentState?.GetType().Name ?? "null";
            string newStateName = newState.GetType().Name;
            Debug.Log($"[HexCellStateMachine] Cell {owner?.Coordinates}: {oldStateName} -> {newStateName}");

            // Execute transition
            currentState?.OnExit(owner);
            currentState = newState;
            currentState.OnEnter(owner);
        }

        /// <summary>
        /// Updates the current state. Called per frame if needed.
        /// Most states won't need updates, but available for animated states.
        /// </summary>
        public void Update()
        {
            currentState?.OnUpdate(owner);
        }

        /// <summary>
        /// Checks if the cell is in a specific state type.
        /// Useful for querying state without type casting.
        /// </summary>
        /// <param name="stateType">The state type to check for</param>
        /// <returns>True if current state matches the type</returns>
        public bool IsInState(HexCellStateType stateType)
        {
            return currentState?.StateType == stateType;
        }
    }
}
