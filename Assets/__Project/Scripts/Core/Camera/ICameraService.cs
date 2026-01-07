namespace Core.Camera
{
    /// <summary>
    /// Service for managing camera transitions between different game states.
    /// Infrastructure layer abstraction for camera control.
    /// </summary>
    public interface ICameraService
    {
        /// <summary>
        /// Switch to combat camera view with smooth transition.
        /// Camera rotates to X:45, Y:90 with adjusted parameters.
        /// </summary>
        /// <param name="transitionTime">Duration of the transition in seconds. If negative, uses config default.</param>
        void SwitchToCombatCamera(float transitionTime = -1f);
        
        /// <summary>
        /// Switch back to isometric camera view with smooth transition.
        /// Camera rotates to X:30, Y:45 with standard parameters.
        /// </summary>
        /// <param name="transitionTime">Duration of the transition in seconds. If negative, uses config default.</param>
        void SwitchToIsometricCamera(float transitionTime = -1f);
        
        /// <summary>
        /// Indicates whether the camera is currently transitioning between views.
        /// </summary>
        bool IsTransitioning { get; }
    }
}

