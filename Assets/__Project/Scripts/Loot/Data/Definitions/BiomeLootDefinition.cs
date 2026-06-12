using LevelGeneration;
using UnityEngine;

namespace Loot.Data.Definitions
{
    /// <summary>
    /// ScriptableObject mapping one level theme (biome) to its artifact loot tables
    /// for the three acquisition paths. Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeLootDefinition", menuName = "Loot/Biome Loot")]
    public class BiomeLootDefinition : ScriptableObject
    {
        [Header("Biome")]
        [SerializeField] private LevelTheme _theme;

        [Header("Platform Discovery")]
        [Tooltip("Chance that a generated platform carries discoverable loot")]
        [Range(0f, 1f)]
        [SerializeField] private float _platformLootChance = 0.5f;

        [Tooltip("Inclusive range of artifacts placed on one loot platform")]
        [SerializeField] private Vector2Int _platformLootCountRange = new Vector2Int(1, 2);

        [SerializeField] private WeightedArtifactEntry[] _platformTable;

        [Header("Enemy Drops (fallback when the enemy defines no loot slots)")]
        [Range(0f, 1f)]
        [SerializeField] private float _enemyDropChance = 0.4f;

        [SerializeField] private WeightedArtifactEntry[] _enemyDropTable;

        [Header("Quest Rewards (procedural component)")]
        [Tooltip("Inclusive range of artifacts granted per completed quest")]
        [SerializeField] private Vector2Int _questRewardCountRange = new Vector2Int(1, 2);

        [SerializeField] private WeightedArtifactEntry[] _questTable;

        public LevelTheme Theme => _theme;
        public float PlatformLootChance => _platformLootChance;
        public Vector2Int PlatformLootCountRange => _platformLootCountRange;
        public WeightedArtifactEntry[] PlatformTable => _platformTable;
        public float EnemyDropChance => _enemyDropChance;
        public WeightedArtifactEntry[] EnemyDropTable => _enemyDropTable;
        public Vector2Int QuestRewardCountRange => _questRewardCountRange;
        public WeightedArtifactEntry[] QuestTable => _questTable;
    }
}
