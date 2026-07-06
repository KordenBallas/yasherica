namespace Core.Persistence
{
    /// <summary>
    /// The pending-restore decision for one Area boot (D3/D6): resolved lazily at first injection —
    /// which is when the run save file is actually read and validated — so every consumer
    /// (seed provider, restore coordinator, entrypoint, presenters) sees one consistent answer to
    /// "is this boot a continue?". A missing/corrupt/stale file simply yields a fresh run.
    /// </summary>
    public sealed class RunRestoreContext
    {
        public RunRestoreContext(RunSaveSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public RunSaveSnapshot Snapshot { get; }

        public bool IsRestoring => Snapshot != null;
    }
}
