namespace Narrative.Threads.Core
{
    /// <summary>
    /// Authored kind of a narrative thread (D13). Ephemeral is the default and is under closure
    /// pressure (it expires when un-advanced past its lifespan); Arc is a long storyline that may
    /// live most of a run and is exempt from expiry — but not from premise-conflict failure.
    /// </summary>
    public enum ThreadKind
    {
        Ephemeral = 0,
        Arc = 1
    }
}
