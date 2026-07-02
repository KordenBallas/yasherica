using UnityEngine;

namespace Narrative.Director.Data
{
    /// <summary>
    /// ScriptableObject world-fullness dials (the world-content-density brief). Configuration data only —
    /// the allocator consumes the mapped Core <c>WorldContentDensitySettings</c>, never this SO. One
    /// asset governs the whole mix: how rare and how spaced quests are, and how the remaining ambient
    /// slots split between empty/traversal, simple loot, and ambient combat.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldContentDensityConfig", menuName = "Narrative/Director/World Content Density Config")]
    public class WorldContentDensityConfig : ScriptableObject
    {
        [Header("Quest Rarity")]
        [Tooltip("~1 quest per this many platforms (a seeded 1-in-N roll per slot)")]
        [Min(1)]
        [SerializeField] private int _averagePlatformsPerQuest = 10;
        [Tooltip("Hard minimum platforms between two quests; 1+ forbids back-to-back quests")]
        [Min(0)]
        [SerializeField] private int _minPlatformsBetweenQuests = 4;

        [Header("Ambient Mix (relative weights)")]
        [Tooltip("Share of non-quest slots that stay empty/traversal (the world's breath)")]
        [Min(0)]
        [SerializeField] private int _emptyWeight = 65;
        [Tooltip("Share of non-quest slots that carry a simple low-tier loot find")]
        [Min(0)]
        [SerializeField] private int _lootWeight = 15;
        [Tooltip("Share of non-quest slots that carry an ambient aggressive monster (biome pool)")]
        [Min(0)]
        [SerializeField] private int _combatWeight = 20;

        [Header("Sites (world-sites brief)")]
        [Tooltip("~1 ambient-channel site (camp/ruin/lair) per this many platforms; 0 disables them")]
        [Min(0)]
        [SerializeField] private int _averagePlatformsPerAmbientSite = 14;
        [Tooltip("Hard minimum platforms between one site block's end and the next site")]
        [Min(0)]
        [SerializeField] private int _minPlatformsBetweenSites = 6;
        [Tooltip("Weight of 'no settlement' when a landed quest rolls against the quest-channel site weights (a lone wanderer)")]
        [Min(0)]
        [SerializeField] private int _wildQuestWeight = 40;

        public int AveragePlatformsPerQuest => _averagePlatformsPerQuest;
        public int MinPlatformsBetweenQuests => _minPlatformsBetweenQuests;
        public int EmptyWeight => _emptyWeight;
        public int LootWeight => _lootWeight;
        public int CombatWeight => _combatWeight;
        public int AveragePlatformsPerAmbientSite => _averagePlatformsPerAmbientSite;
        public int MinPlatformsBetweenSites => _minPlatformsBetweenSites;
        public int WildQuestWeight => _wildQuestWeight;
    }
}
