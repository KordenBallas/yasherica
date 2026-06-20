using UnityEngine;
using System.Collections.Generic;
using Loot.Data.Definitions;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for enemy configurations.
    /// References other definitions for abilities and AI profile.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Combat/Enemies/Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this enemy type")]
        [SerializeField] private int _enemyId;

        [Tooltip("Display name of the enemy")]
        [SerializeField] private string _name;

        [TextArea(2, 4)]
        [Tooltip("Description of the enemy")]
        [SerializeField] private string _description;

        [Header("Stats")]
        [Tooltip("Maximum hit points")]
        [SerializeField] private int _maxHP = 50;

        [Header("Visual")]
        [Tooltip("Prefab for enemy GameObject (optional - can use default)")]
        [SerializeField] private GameObject _prefab;

        [Header("Abilities")]
        [Tooltip("List of ability definitions this enemy can use")]
        [SerializeField] private List<AbilityDefinition> _abilities;

        [Header("AI Configuration")]
        [Tooltip("AI behavior profile for this enemy")]
        [SerializeField] private AIProfileDefinition _aiProfile;

        [Header("Loot")]
        [Tooltip("Independently-rolled artifact drops on defeat; empty falls back to the biome enemy drop table")]
        [SerializeField] private List<ArtifactLootSlot> _lootSlots;

        [Header("Narrative Matching")]
        [Tooltip("Semantic tags used to match this enemy into a story's combat slot (e.g. 'bandit')")]
        [SerializeField] private List<string> _enemyTags = new List<string>();

        // Public read-only accessors
        public int EnemyId => _enemyId;
        public string Name => _name;
        public string Description => _description;
        public int MaxHP => _maxHP;
        public GameObject Prefab => _prefab;
        public IReadOnlyList<AbilityDefinition> Abilities => _abilities ?? new List<AbilityDefinition>();
        public AIProfileDefinition AIProfile => _aiProfile;
        public IReadOnlyList<ArtifactLootSlot> LootSlots =>
            _lootSlots ?? (IReadOnlyList<ArtifactLootSlot>)System.Array.Empty<ArtifactLootSlot>();
        public IReadOnlyList<string> EnemyTags =>
            _enemyTags ?? (IReadOnlyList<string>)System.Array.Empty<string>();
    }
}
