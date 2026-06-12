namespace Loot.Core
{
    /// <summary>
    /// Holds the effective seed of the current generated run.
    /// Set once per area generation; consumed by deterministic loot rolls.
    /// </summary>
    public interface IRunSeedProvider
    {
        int RunSeed { get; }

        void SetSeed(int seed);
    }
}
