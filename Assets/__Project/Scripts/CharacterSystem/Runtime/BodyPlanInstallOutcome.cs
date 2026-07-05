namespace CharacterSystem.Runtime
{
    /// <summary>Immediate outcome of a part-install request routed through the body-plan
    /// coordinator.</summary>
    public enum BodyPlanInstallOutcome
    {
        /// <summary>The install completed synchronously (instant swap, dormant install, or a
        /// shed-nothing frame change).</summary>
        Applied,

        /// <summary>A confirm prompt is up; the final result arrives via
        /// <see cref="BodyPlanSwapCoordinator.InstallResolved"/>.</summary>
        PendingConfirmation,

        /// <summary>The install cannot be performed (incompatible part, unknown ids, or a
        /// failed build); nothing was changed.</summary>
        Rejected
    }
}
