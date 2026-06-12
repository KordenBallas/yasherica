using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Holds the biome theme of the currently generated area so runtime loot
    /// rolls (enemy drops, quest rewards) can pick the right biome tables.
    /// Set once per area generation.
    /// </summary>
    public interface ICurrentThemeProvider
    {
        LevelTheme CurrentTheme { get; }

        void SetTheme(LevelTheme theme);
    }
}
