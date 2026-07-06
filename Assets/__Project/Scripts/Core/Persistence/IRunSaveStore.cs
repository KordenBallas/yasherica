namespace Core.Persistence
{
    /// <summary>
    /// The one implicit in-progress run save (A1: no slots, no manual saves). <see cref="Exists"/>
    /// drives the Continue entry point; <see cref="Delete"/> is how a death (or an explicit new
    /// Journey) consumes the run (FR2). A corrupt file is discarded on load — the player starts a
    /// fresh run, never crashes (FR13).
    /// </summary>
    public interface IRunSaveStore
    {
        bool Exists();
        bool TryLoad(out RunSaveSnapshot snapshot);
        void Save(RunSaveSnapshot snapshot);
        void Delete();
    }
}
