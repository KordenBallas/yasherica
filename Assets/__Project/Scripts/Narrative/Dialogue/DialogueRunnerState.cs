namespace Narrative.Dialogue
{
    /// <summary>
    /// Explicit suspension states for the dialogue runner (B1). Ink is synchronous, so an async
    /// outcome (combat) must park the conversation in <see cref="AwaitingExternal"/> — no further Ink
    /// is pumped — until the result is reported and the runner resumes with exactly one continue.
    /// </summary>
    public enum DialogueRunnerState
    {
        Running = 0,
        AwaitingExternal = 1,
        Ended = 2
    }
}
