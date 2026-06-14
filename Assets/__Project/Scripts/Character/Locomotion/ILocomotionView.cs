namespace Character.Locomotion
{
    /// <summary>
    /// Output side of the locomotion MVP: applies the solver's results to Unity visuals.
    /// Implemented by a thin MonoBehaviour adapter; contains no logic of its own.
    /// </summary>
    public interface ILocomotionView
    {
        /// <summary>Sets the animator blend parameter (0 = idle, 1 = full run).</summary>
        void SetMotionSpeed(float normalizedSpeed);

        /// <summary>Yaws the character (Y-axis only) to the given heading in degrees.</summary>
        void SetFacingYaw(float yawDegrees);
    }
}
