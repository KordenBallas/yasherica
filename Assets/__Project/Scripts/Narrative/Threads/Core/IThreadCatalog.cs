namespace Narrative.Threads.Core
{
    /// <summary>
    /// Read-only view of the authored thread vocabulary. A thread label used by a story without a
    /// declared <c>ThreadDefinition</c> resolves to an implicit ephemeral default (FR2: ephemeral is
    /// the default), so existing bare-labelled content keeps working without new assets.
    /// </summary>
    public interface IThreadCatalog
    {
        bool TryGet(string threadId, out ThreadDefinitionData definition);

        /// <summary>The declared definition, or the implicit ephemeral default (empty premise, the
        /// tuning-level default lifespan) for an undeclared label.</summary>
        ThreadDefinitionData GetOrImplicitDefault(string threadId);
    }
}
