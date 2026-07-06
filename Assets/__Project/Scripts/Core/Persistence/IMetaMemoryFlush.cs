namespace Core.Persistence
{
    /// <summary>
    /// Writes the current Meta-horizon facts out to the cross-run memory file. Called at the
    /// savepoint boundaries and — crucially, before the run save is deleted — on death (D9: flush
    /// points, not write-through).
    /// </summary>
    public interface IMetaMemoryFlush
    {
        void Flush();
    }
}
