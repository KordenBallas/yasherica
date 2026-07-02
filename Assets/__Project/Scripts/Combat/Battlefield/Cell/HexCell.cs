using System;
using Core.Logging;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Model class for a hexagonal cell on the battlefield.
    /// Pure C# - no Unity dependencies except Vector3 and Color (data types).
    /// Now includes state machine for explicit state management.
    /// </summary>
    public class HexCell : IHexCell
    {
        private readonly HexCellStateMachine stateMachine;
        private Color legacyColor; // For backward compatibility
        private readonly IGameLogger _logger;

        // Immutable properties
        public HexCoordinates Coordinates { get; }
        public Vector3 WorldPosition { get; }

        // State management
        public HexCellStateMachine StateMachine => stateMachine;
        public bool IsActive { get; set; }

        // Legacy support (deprecated)
        [Obsolete("Use StateMachine.CurrentState.GetColor() instead. This property is deprecated and will be removed in a future version.")]
        public Color Color
        {
            get => stateMachine?.CurrentState?.GetColor() ?? legacyColor;
            set
            {
                legacyColor = value;
                // Fire event for backward compatibility
                OnStateChanged?.Invoke(stateMachine?.CurrentState);
            }
        }

        // Events
        public event Action<IHexCellState> OnStateChanged;

        public HexCell(HexCoordinates coordinates, Vector3 worldPosition, Color? initialColor = null, IGameLogger logger = null)
        {
            Coordinates = coordinates;
            WorldPosition = worldPosition;
            legacyColor = initialColor ?? Color.white;
            IsActive = true;
            _logger = logger;

            stateMachine = new HexCellStateMachine(_logger);
        }

        public void InitializeStateMachine(IHexCellState initialState)
        {
            if (initialState == null)
            {
                _logger?.Error(LogCategory.Combat, $"[HexCell] {Coordinates}: Cannot initialize with null state");
                return;
            }

            stateMachine.Initialize(this, initialState);
            OnStateChanged?.Invoke(initialState);
        }

        public void ChangeState(IHexCellState newState)
        {
            if (newState == null)
            {
                _logger?.Warning(LogCategory.Combat, $"[HexCell] {Coordinates}: Attempted to change to null state");
                return;
            }

            stateMachine.ChangeState(newState);
            OnStateChanged?.Invoke(newState);
        }
    }
}

