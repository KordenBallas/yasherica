namespace Core.Persistence
{
    /// <summary>
    /// Notified once per taken savepoint, after the run image is written and before the meta
    /// partition flushes — so an observer that writes a meta fact (e.g. the Heat high-water record)
    /// has it persisted by that same flush. Observers must be cheap and must not mutate run state.
    /// </summary>
    public interface ISavepointObserver
    {
        void OnSavepointCaptured(RunSaveSnapshot snapshot);
    }
}
