using System;
using Combat.Input.Commands;
using UnityEngine;

namespace Combat.Input
{
    /// <summary>
    /// Platform-agnostic input controller interface.
    /// Implementations provide device-specific input handling and emit commands via events.
    /// </summary>
    public interface IInputController
    {
        // ===== EVENTS (Command Emission) =====

        /// <summary>
        /// Fired when movement mode is activated or deactivated.
        /// </summary>
        event Action<MovementModeChangedCommand> OnMovementModeChanged;

        /// <summary>
        /// Fired when the movement direction changes (only while movement mode is active).
        /// </summary>
        event Action<MovementDirectionChangedCommand> OnMovementDirectionChanged;

        /// <summary>
        /// Fired when the player confirms the current movement.
        /// </summary>
        event Action<MovementConfirmedCommand> OnMovementConfirmed;

        /// <summary>
        /// Fired when the player cancels the current movement.
        /// </summary>
        event Action<MovementCancelledCommand> OnMovementCancelled;

        // ===== LEGACY PROPERTIES (Deprecated - Keep for migration) =====

        /// <summary>
        /// Returns true if the player is actively requesting movement input.
        /// </summary>
        [Obsolete("Use OnMovementModeChanged event instead. Will be removed in future version.")]
        bool IsMovementModeActive { get; }

        /// <summary>
        /// Returns normalized world direction vector, or null if no direction input.
        /// Direction is in world space (XZ plane).
        /// </summary>
        [Obsolete("Use OnMovementDirectionChanged event instead. Will be removed in future version.")]
        Vector3? GetMovementDirection();

        /// <summary>
        /// Returns true if the player confirms the current action.
        /// </summary>
        [Obsolete("Use OnMovementConfirmed event instead. Will be removed in future version.")]
        bool IsConfirmPressed { get; }

        /// <summary>
        /// Returns true if the player cancels the current action.
        /// </summary>
        [Obsolete("Use OnMovementCancelled event instead. Will be removed in future version.")]
        bool IsCancelPressed { get; }

        // ===== CONTROL METHODS =====

        /// <summary>
        /// Enables input processing.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables input processing.
        /// </summary>
        void Disable();
    }
}
