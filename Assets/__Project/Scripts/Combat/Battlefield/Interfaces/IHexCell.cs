using System;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Interface for hex cell model.
    /// Maintains immutable properties and state machine.
    /// </summary>
    public interface IHexCell
    {
        // Immutable properties
        HexCoordinates Coordinates { get; }
        Vector3 WorldPosition { get; }

        // State management
        HexCellStateMachine StateMachine { get; }
        bool IsActive { get; set; }

        // Legacy support (deprecated)
        [Obsolete("Use StateMachine.CurrentState.GetColor() instead. This property is deprecated and will be removed in a future version.")]
        Color Color { get; set; }

        // Events
        /// <summary>
        /// Fired when the cell's state changes.
        /// Subscribers receive the new state.
        /// </summary>
        event Action<IHexCellState> OnStateChanged;

        /// <summary>
        /// Initializes the cell's state machine with an initial state.
        /// Should be called during cell creation.
        /// </summary>
        /// <param name="initialState">The initial state for this cell</param>
        void InitializeStateMachine(IHexCellState initialState);

        /// <summary>
        /// Changes the cell's state and fires the OnStateChanged event.
        /// </summary>
        /// <param name="newState">The new state to transition to</param>
        void ChangeState(IHexCellState newState);
    }
}

