namespace Narrative.Director.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free world-fullness dials for the windowed director (the
    /// world-content-density brief): how rare quests are (<see cref="AveragePlatformsPerQuest"/> with a
    /// hard <see cref="MinPlatformsBetweenQuests"/> spacing), and how the remaining ambient slots split
    /// between empty/traversal, simple loot, and ambient combat by integer weight. Mapped from the
    /// <c>WorldContentDensityConfig</c> SO at install time.
    /// </summary>
    public sealed class WorldContentDensitySettings
    {
        public int AveragePlatformsPerQuest { get; }
        public int MinPlatformsBetweenQuests { get; }
        public int EmptyWeight { get; }
        public int LootWeight { get; }
        public int CombatWeight { get; }

        public WorldContentDensitySettings(int averagePlatformsPerQuest, int minPlatformsBetweenQuests,
            int emptyWeight, int lootWeight, int combatWeight)
        {
            AveragePlatformsPerQuest = averagePlatformsPerQuest < 1 ? 1 : averagePlatformsPerQuest;
            MinPlatformsBetweenQuests = minPlatformsBetweenQuests < 0 ? 0 : minPlatformsBetweenQuests;
            EmptyWeight = emptyWeight < 0 ? 0 : emptyWeight;
            LootWeight = lootWeight < 0 ? 0 : lootWeight;
            CombatWeight = combatWeight < 0 ? 0 : combatWeight;
        }
    }
}
