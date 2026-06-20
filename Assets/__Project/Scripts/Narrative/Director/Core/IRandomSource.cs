namespace Narrative.Director.Core
{
    /// <summary>
    /// Deterministic random source for narrative procedural choices (director selection, casting
    /// tie-break, name-pool draws). Its full state is serializable (B2) so a save→reload→continue
    /// replay matches an uninterrupted run.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>A non-negative int in [0, maxExclusive). Returns 0 if maxExclusive &lt;= 1.</summary>
        int NextInt(int maxExclusive);

        /// <summary>The current internal state (captured/restored for save-load).</summary>
        ulong State { get; set; }
    }
}
