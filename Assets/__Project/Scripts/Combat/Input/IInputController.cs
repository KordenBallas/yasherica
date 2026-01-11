using UnityEngine;

namespace Combat.Input
{
    /// <summary>
    /// Platform-agnostic input controller interface.
    /// Implementations provide device-specific input handling.
    /// </summary>
    public interface IInputController
    {
        /// <summary>
        /// Returns true if the player is actively requesting movement input.
        /// </summary>
        bool IsMovementModeActive { get; }
        
        /// <summary>
        /// Returns normalized world direction vector, or null if no direction input.
        /// Direction is in world space (XZ plane).
        /// </summary>
        Vector3? GetMovementDirection();
        
        /// <summary>
        /// Returns true if the player confirms the current action.
        /// </summary>
        bool IsConfirmPressed { get; }
        
        /// <summary>
        /// Returns true if the player cancels the current action.
        /// </summary>
        bool IsCancelPressed { get; }
        
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
