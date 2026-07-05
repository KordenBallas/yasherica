namespace Narrative.Facts.Core
{
    /// <summary>
    /// Lifetime horizon of a fact key (D20). Deliberately separate from <see cref="FactScope"/>
    /// (subject arity) and <see cref="FactNamespace"/> (grouping label): horizon answers "when does
    /// this fact reset", not "who is it about". The director keeps the two horizons cleanly
    /// partitioned so save/load (P2-2) can persist each side separately; cross-run persistence and
    /// its consumers are out of scope here.
    /// </summary>
    public enum FactHorizon
    {
        /// <summary>Run-scoped: the fact resets on death (the default).</summary>
        Run = 0,

        /// <summary>Meta-scoped: the fact persists across runs (spine cursor, mirror-lore flags,
        /// cauldron memory).</summary>
        Meta = 1
    }
}
