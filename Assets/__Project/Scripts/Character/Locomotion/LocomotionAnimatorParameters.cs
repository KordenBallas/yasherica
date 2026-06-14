namespace Character.Locomotion
{
    /// <summary>
    /// Single source of truth for the locomotion animator parameter names, shared by the
    /// runtime view that writes them and the editor tooling that authors the controller.
    /// </summary>
    public static class LocomotionAnimatorParameters
    {
        /// <summary>Float blend parameter (0 = idle, 1 = full run) driving the locomotion blend tree.</summary>
        public const string Speed = "Speed";
    }
}
