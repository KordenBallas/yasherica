namespace MetaProgression.Core
{
    /// <summary>
    /// The unlocked cross-run vocabulary (meta-progression FR1/FR3): answers whether a content
    /// token (a part form, artifact, recipe, pool) is currently in the possibility space. Built
    /// once per scene from the meta store on disk and frozen for the run (FR13) — a deed done in
    /// run N changes run N+1's answers, never the current run's pools.
    /// </summary>
    public interface IMetaVocabulary
    {
        /// <summary>
        /// True when the token may appear in draws: base tokens always; meta-gated tokens once
        /// their deed is met and their unlock tier's run-floor is reached. A null gate reads as
        /// the unmarked default (base).
        /// </summary>
        bool IsUnlocked(string tokenId, MetaGate gate);
    }
}
