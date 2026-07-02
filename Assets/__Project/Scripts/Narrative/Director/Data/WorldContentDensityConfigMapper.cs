using Narrative.Director.Core;

namespace Narrative.Director.Data
{
    /// <summary>
    /// The only bridge from the <see cref="WorldContentDensityConfig"/> SO to the UnityEngine-free
    /// <see cref="WorldContentDensitySettings"/> Core record (CLAUDE.md §7). Falls back to the same
    /// defaults as the SO when no config asset is wired.
    /// </summary>
    public static class WorldContentDensityConfigMapper
    {
        public static WorldContentDensitySettings ToSettings(WorldContentDensityConfig config)
        {
            if (config == null)
            {
                return new WorldContentDensitySettings(averagePlatformsPerQuest: 10,
                    minPlatformsBetweenQuests: 4, emptyWeight: 65, lootWeight: 15, combatWeight: 20);
            }

            return new WorldContentDensitySettings(
                config.AveragePlatformsPerQuest,
                config.MinPlatformsBetweenQuests,
                config.EmptyWeight,
                config.LootWeight,
                config.CombatWeight,
                config.AveragePlatformsPerAmbientSite,
                config.MinPlatformsBetweenSites,
                config.WildQuestWeight);
        }
    }
}
