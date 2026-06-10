using UnityEngine;
using System.Collections.Generic;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for hero configurations.
    /// References ability definitions for hero's available abilities.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroDefinition", menuName = "Combat/Heroes/Hero")]
    public class HeroDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this hero type")]
        [SerializeField] private int _heroId;

        [Tooltip("Display name of the hero")]
        [SerializeField] private string _name;

        [TextArea(2, 4)]
        [Tooltip("Description of the hero")]
        [SerializeField] private string _description;

        [Header("Stats")]
        [Tooltip("Maximum hit points")]
        [SerializeField] private int _maxHP = 100;

        [Header("Visual")]
        [Tooltip("Prefab for hero GameObject (optional - can use existing character)")]
        [SerializeField] private GameObject _prefab;

        [Header("Abilities")]
        [Tooltip("List of ability definitions this hero can use")]
        [SerializeField] private List<AbilityDefinition> _abilities;

        // Public read-only accessors
        public int HeroId => _heroId;
        public string Name => _name;
        public string Description => _description;
        public int MaxHP => _maxHP;
        public GameObject Prefab => _prefab;
        public IReadOnlyList<AbilityDefinition> Abilities => _abilities ?? new List<AbilityDefinition>();
    }
}
