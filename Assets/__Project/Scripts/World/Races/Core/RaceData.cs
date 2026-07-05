using LevelGeneration;

namespace World.Races.Core
{
    /// <summary>
    /// UnityEngine-free record of one race: its id (the passport tag on body parts and the fact
    /// subject), display name, and home biome. The belonging colour stays on the authoring SO
    /// (Core records never hold Unity types, CLAUDE.md §7); presentation reads it there.
    /// </summary>
    public sealed class RaceData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public LevelTheme HomeBiome { get; }

        public RaceData(string id, string displayName, LevelTheme homeBiome)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            HomeBiome = homeBiome;
        }
    }
}
