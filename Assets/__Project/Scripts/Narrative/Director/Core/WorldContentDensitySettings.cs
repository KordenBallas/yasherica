namespace Narrative.Director.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free world-fullness dials for the windowed director (the
    /// world-content-density brief, extended by the sites brief): how rare quests are
    /// (<see cref="AveragePlatformsPerQuest"/> with a hard <see cref="MinPlatformsBetweenQuests"/>
    /// spacing), how the remaining ambient slots split between empty/traversal, simple loot, and
    /// ambient combat by integer weight, and how rare site blocks are — the ambient-channel gate
    /// (<see cref="AveragePlatformsPerAmbientSite"/> / <see cref="MinPlatformsBetweenSites"/>) plus
    /// the quest-channel wild-vs-settlement roll (<see cref="WildQuestWeight"/>). Mapped from the
    /// <c>WorldContentDensityConfig</c> SO at install time.
    /// </summary>
    public sealed class WorldContentDensitySettings
    {
        private const int DefaultAveragePlatformsPerAmbientSite = 14;
        private const int DefaultMinPlatformsBetweenSites = 6;
        private const int DefaultWildQuestWeight = 40;

        public int AveragePlatformsPerQuest { get; }
        public int MinPlatformsBetweenQuests { get; }
        public int EmptyWeight { get; }
        public int LootWeight { get; }
        public int CombatWeight { get; }

        /// <summary>1-in-N rarity of the ambient site roll; 0 disables ambient sites entirely.</summary>
        public int AveragePlatformsPerAmbientSite { get; }

        /// <summary>Hard minimum of non-site platforms between one site block's end and the next site.</summary>
        public int MinPlatformsBetweenSites { get; }

        /// <summary>Weight of "no settlement" in the quest-channel roll against the site trigger weights.</summary>
        public int WildQuestWeight { get; }

        public WorldContentDensitySettings(int averagePlatformsPerQuest, int minPlatformsBetweenQuests,
            int emptyWeight, int lootWeight, int combatWeight,
            int averagePlatformsPerAmbientSite = DefaultAveragePlatformsPerAmbientSite,
            int minPlatformsBetweenSites = DefaultMinPlatformsBetweenSites,
            int wildQuestWeight = DefaultWildQuestWeight)
        {
            AveragePlatformsPerQuest = averagePlatformsPerQuest < 1 ? 1 : averagePlatformsPerQuest;
            MinPlatformsBetweenQuests = minPlatformsBetweenQuests < 0 ? 0 : minPlatformsBetweenQuests;
            EmptyWeight = emptyWeight < 0 ? 0 : emptyWeight;
            LootWeight = lootWeight < 0 ? 0 : lootWeight;
            CombatWeight = combatWeight < 0 ? 0 : combatWeight;
            AveragePlatformsPerAmbientSite = averagePlatformsPerAmbientSite < 0 ? 0 : averagePlatformsPerAmbientSite;
            MinPlatformsBetweenSites = minPlatformsBetweenSites < 0 ? 0 : minPlatformsBetweenSites;
            WildQuestWeight = wildQuestWeight < 0 ? 0 : wildQuestWeight;
        }
    }
}
