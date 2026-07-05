namespace Mutation.Core
{
    /// <summary>Immediate outcome of an unseal part-install request. Mirrors the character
    /// system's install outcome without referencing it (the port stays layer-clean).</summary>
    public enum SwapRequestOutcome
    {
        /// <summary>The part is on the body (or carried dormant); commit the unseal.</summary>
        Applied,

        /// <summary>A body-plan confirm dialog is up; the final result arrives via
        /// <see cref="IMutationCharacter.SwapRequestResolved"/>.</summary>
        PendingConfirmation,

        /// <summary>The install cannot be performed; nothing changed (keep the cards up).</summary>
        Rejected
    }
}
