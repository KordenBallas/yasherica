namespace Core.Persistence
{
    /// <summary>
    /// The always-on cross-run memory file (FR9). Load never fails: a missing or unreadable file
    /// degrades to an empty world-memory (blank slate, as if a first-ever run) rather than
    /// crashing (FR13) — corrupt content is quarantined, not silently destroyed.
    /// </summary>
    public interface IMetaMemoryStore
    {
        MetaMemorySnapshot LoadOrEmpty();
        void Save(MetaMemorySnapshot snapshot);
    }
}
