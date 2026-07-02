using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// The ambient-monster pool for one biome theme (the world-content-density brief): the enemies an
    /// ambient combat platform may draw, at flat difficulty. Configuration data only — the allocator
    /// consumes the mapped enemy-id pool via <c>IBiomeMonsterPoolCatalog</c>, never this SO.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterPool_", menuName = "Combat/Enemies/Biome Monster Pool")]
    public class BiomeMonsterPoolDefinition : ScriptableObject
    {
        [Tooltip("The biome this pool belongs to")]
        [SerializeField] private LevelTheme _theme;
        [Tooltip("Enemies an ambient combat platform in this biome may spawn (flat difficulty)")]
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();

        public LevelTheme Theme => _theme;
        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;
    }
}
