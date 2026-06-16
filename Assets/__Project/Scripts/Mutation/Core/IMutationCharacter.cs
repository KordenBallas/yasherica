namespace Mutation.Core
{
    /// <summary>
    /// UnityEngine-free port the stage-up mutation choice uses to mutate the live character, so the
    /// presenter never touches a MonoBehaviour. An infrastructure adapter implements it over the
    /// assembled modular character; the swap is resolved lazily, so a not-yet-assembled rig simply
    /// fails the swap (returns false) instead of throwing.
    /// </summary>
    public interface IMutationCharacter
    {
        /// <summary>
        /// Swaps the body part in <paramref name="slotId"/> for <paramref name="partId"/>.
        /// Returns false (and logs) when the swap cannot be applied - e.g. the rig is not yet
        /// assembled or the part/slot is unknown.
        /// </summary>
        bool SwapPart(string slotId, string partId);

        /// <summary>
        /// The part currently equipped in <paramref name="slotId"/> as far as this port knows.
        /// Used to keep an already-equipped part from being offered again. Starting parts the
        /// player never mutated into may be unknown (returns false), which is acceptable for M1.
        /// </summary>
        bool TryGetEquippedPartId(string slotId, out string partId);
    }
}
