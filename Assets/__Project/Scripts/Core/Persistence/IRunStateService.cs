using Narrative.Dialogue;

namespace Core.Persistence
{
    /// <summary>
    /// The whole-run capture/restore aggregator (FR5: one coherent image, not per-system fragments).
    /// Capture honors the W3-1 savepoint guard via the narrative save service; restore replays the
    /// sections in dependency order.
    /// </summary>
    public interface IRunStateService
    {
        /// <summary>Captures the full run image, or returns false when the current moment is not a
        /// savepoint (a dialogue awaiting an external step, W3-1).</summary>
        bool TryCaptureAll(DialogueRunnerState dialogueState, out RunSaveSnapshot snapshot);

        /// <summary>Replays a loaded run image into the live services. The world section is NOT
        /// applied here — the Area entrypoint rebuilds the world through the streaming coordinator
        /// before placing the hero.</summary>
        void RestoreAll(RunSaveSnapshot snapshot);
    }
}
