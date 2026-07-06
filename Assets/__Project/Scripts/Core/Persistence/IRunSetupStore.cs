namespace Core.Persistence
{
    /// <summary>
    /// The one-shot run-setup carrier (<c>run-setup.json</c>): the Hub writes it at launch, the next
    /// fresh Area boot consumes it (load + delete). A corrupt or foreign-version file is discarded —
    /// the run simply starts with defaults (bare hero, seeded biome), never crashes.
    /// </summary>
    public interface IRunSetupStore
    {
        bool TryLoad(out RunSetupSnapshot snapshot);
        void Save(RunSetupSnapshot snapshot);
        void Delete();
    }
}
