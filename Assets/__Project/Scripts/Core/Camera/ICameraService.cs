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
        /// Registers the world anchor the belly camera follows while active.
        /// Provided by the hero (IBellyAnchorProvider) so the framing tracks the
        /// character even while it rotates.
        /// </summary>
        void SetBellyAnchor(UnityEngine.Transform anchor);

        /// <summary>
        /// Switch to the belly close-up camera (magic pot inventory view).
        /// </summary>
        /// <param name="transitionTime">Duration of the transition in seconds. If negative, uses config default.</param>
        void SwitchToBellyCamera(float transitionTime = -1f);

        /// <summary>
        /// Switch back to whichever camera (isometric or combat) was active
        /// before the belly camera took over.
        /// </summary>
        /// <param name="transitionTime">Duration of the transition in seconds. If negative, uses config default.</param>
        void SwitchToPreviousCamera(float transitionTime = -1f);

        /// <summary>
        /// Indicates whether the camera is currently transitioning between views.
        /// </summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// World position of the camera that actually renders the frame (the
        /// Cinemachine brain output), e.g. for turning the character toward it.
        /// </summary>
        UnityEngine.Vector3 OutputCameraPosition { get; }
    }
}

