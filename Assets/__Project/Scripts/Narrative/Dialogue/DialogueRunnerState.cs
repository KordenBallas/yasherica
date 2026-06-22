namespace Narrative.Dialogue
{
    /// <summary>
    /// Explicit suspension states for the dialogue runner. Ink is synchronous, so the runner parks the
    /// conversation rather than pumping straight through:
    /// <list type="bullet">
    /// <item><see cref="AwaitingExternal"/> (B1) — an async outcome (combat) suspends pumping until the
    /// result is reported and the runner resumes with exactly one continue.</item>
    /// <item><see cref="AwaitingContinue"/> — a readable line was emitted; pumping waits for the player's
    /// continue input (via <see cref="DialogueRunner.Continue"/>) so multi-line knots are read one line
    /// at a time instead of collapsing to the last line.</item>
    /// </list>
    /// </summary>
    public enum DialogueRunnerState
    {
        Running = 0,
        AwaitingExternal = 1,
        Ended = 2,
        AwaitingContinue = 3
    }
}
