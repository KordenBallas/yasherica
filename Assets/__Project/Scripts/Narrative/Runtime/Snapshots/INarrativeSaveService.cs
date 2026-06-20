using Narrative.Dialogue;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Boundary for persisting/restoring all narrative run-state (R14). The file-IO implementation is a
    /// ROADMAP item; the contract and the suspended-session guard (W3-1) are defined here.
    /// </summary>
    public interface INarrativeSaveService
    {
        /// <summary>
        /// True if a snapshot may be taken now. Per W3-1 (option a) a dialogue parked in
        /// <see cref="DialogueRunnerState.AwaitingExternal"/> is a NON-savepoint, because Ink state does
        /// not capture the pending-external descriptor — so capture is refused/deferred.
        /// </summary>
        bool CanCapture(DialogueRunnerState dialogueState);

        /// <summary>Attempts to capture a snapshot; returns false (and leaves <paramref name="snapshot"/>
        /// null) when <see cref="CanCapture"/> is false.</summary>
        bool TryCapture(DialogueRunnerState dialogueState, out RunNarrativeSnapshot snapshot);
    }
}
