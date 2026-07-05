using System;

namespace Mutation.Core
{
    /// <summary>
    /// UnityEngine-free port the stage-up mutation choice uses to mutate the live character, so the
    /// presenter never touches a MonoBehaviour. An infrastructure adapter implements it over the
    /// assembled modular character; everything is resolved lazily, so a not-yet-assembled rig simply
    /// rejects the request instead of throwing.
    ///
    /// Since body plans (P2-1) an install is a REQUEST: a frame-changing part that would shed
    /// equipped parts first shows a confirm dialog, so the outcome can be deferred
    /// (<see cref="SwapRequestOutcome.PendingConfirmation"/> + <see cref="SwapRequestResolved"/>).
    /// </summary>
    public interface IMutationCharacter
    {
        /// <summary>
        /// Requests installing <paramref name="partId"/> into <paramref name="slotId"/>.
        /// Applied = done synchronously (commit the unseal). PendingConfirmation = a body-plan
        /// confirm dialog is up; await <see cref="SwapRequestResolved"/>. Rejected = nothing
        /// changed (e.g. the rig is not yet assembled or the part does not fit).
        /// </summary>
        SwapRequestOutcome RequestSwapPart(string slotId, string partId);

        /// <summary>Final result of a request that returned
        /// <see cref="SwapRequestOutcome.PendingConfirmation"/>: true = installed (commit),
        /// false = declined or failed (the body, blank, and sockets are untouched).</summary>
        event Action<bool> SwapRequestResolved;

        /// <summary>True when the part could be installed on the current body right now.
        /// Used to keep parts that do not fit the governing frame out of the unseal offers.</summary>
        bool CanInstall(string partId);

        /// <summary>
        /// The part currently occupying <paramref name="slotId"/> on the live character —
        /// active or carried dormant (a losing frame-changer keeps its slot). Used to keep an
        /// already-equipped part from being offered again. Returns false only when the slot is
        /// empty/unknown or the rig is not yet assembled.
        /// </summary>
        bool TryGetEquippedPartId(string slotId, out string partId);
    }
}
