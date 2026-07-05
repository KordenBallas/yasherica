using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Holds the biome theme of the run's active biome stretch so live readers (loot rolls, the
    /// planner's monster-pool draws, platform loot) pick the right biome tables. Set per stretch by
    /// the biome journey's <c>BiomeStretchDirector</c> — the single owner of writes.
    /// </summary>
    public interface ICurrentThemeProvider
    {
        LevelTheme CurrentTheme { get; }

        void SetTheme(LevelTheme theme);
    }
}
