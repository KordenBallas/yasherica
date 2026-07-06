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
        event Action<MovementModeChangedCommand> OnMovementModeChanged;
        event Action<MovementDirectionChangedCommand> OnMovementDirectionChanged;
        event Action<MovementConfirmedCommand> OnMovementConfirmed;
        event Action<MovementCancelledCommand> OnMovementCancelled;
        event Action<AbilitySelectedCommand> OnAbilitySelected;
        event Action<AbilityCancelledCommand> OnAbilityCancelled;

        /// <summary>
        /// Fired when the player releases an ability key to confirm targeting.
        /// </summary>
        event Action<AbilityConfirmedCommand> OnAbilityConfirmed;

        event Action<ExecuteQueueCommand> OnExecuteQueueRequested;
        event Action<ChangeDirectionModeCommand> OnChangeDirectionRequested;

        /// <summary>
        /// Fired when the player starts aiming the queued volley (holds the execute key): the hero turns
        /// toward the cursor while held, and the queue fires (<see cref="OnExecuteQueueRequested"/>) on
        /// release unless aborted by <see cref="OnVolleyAimCancelled"/> (D2).
        /// </summary>
        event Action<VolleyAimStartedCommand> OnVolleyAimStarted;

        /// <summary>Fired when an in-progress volley aim is aborted (right-click while holding); the release does not fire the queue.</summary>
        event Action<VolleyAimCancelledCommand> OnVolleyAimCancelled;

        // ===== LEGACY PROPERTIES (Deprecated) =====

        [Obsolete("Use OnMovementModeChanged event instead.")]
        bool IsMovementModeActive { get; }

        [Obsolete("Use OnMovementDirectionChanged event instead.")]
        Vector3? GetMovementDirection();

        [Obsolete("Use OnMovementConfirmed event instead.")]
        bool IsConfirmPressed { get; }

        [Obsolete("Use OnMovementCancelled event instead.")]
        bool IsCancelPressed { get; }

        void Enable();
        void Disable();
    }
}
