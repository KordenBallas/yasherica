using System.Collections.Generic;
using System.Linq;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using UnityEngine;

namespace Combat.Data.Providers
{
    /// <summary>
    /// IEnemyDataProvider implementation using ScriptableObject definitions.
    /// Replaces SimpleEnemyDataProvider for production use.
    /// </summary>
    public class ScriptableObjectEnemyDataProvider : IEnemyDataProvider
    {
        private readonly Dictionary<int, EnemyDefinition> _enemyDefinitions;
        private readonly IAbilityFactory _abilityFactory;

        public ScriptableObjectEnemyDataProvider(
            IReadOnlyList<EnemyDefinition> enemyDefinitions,
            IAbilityFactory abilityFactory)
        {
            _abilityFactory = abilityFactory;
            _enemyDefinitions = new Dictionary<int, EnemyDefinition>();

            if (enemyDefinitions == null || enemyDefinitions.Count == 0)
            {
                Debug.LogWarning("[ScriptableObjectEnemyDataProvider] No enemy definitions provided");
                return;
            }

            foreach (var definition in enemyDefinitions)
            {
                if (definition == null)
                    continue;

                if (_enemyDefinitions.ContainsKey(definition.EnemyId))
                {
                    Debug.LogWarning(
                        $"[ScriptableObjectEnemyDataProvider] Duplicate enemy ID {definition.EnemyId}: " +
                        $"'{definition.Name}' conflicts with existing enemy");
                    continue;
                }

                _enemyDefinitions[definition.EnemyId] = definition;
            }
        }

        public EnemyData GetEnemyData(int enemyId)
        {
            if (!_enemyDefinitions.TryGetValue(enemyId, out var definition))
            {
                Debug.LogWarning(
                    $"[ScriptableObjectEnemyDataProvider] Enemy {enemyId} not found, returning default");
                return CreateDefaultEnemyData(enemyId);
            }

            return ConvertToEnemyData(definition);
        }

        /// <summary>
        /// Gets the raw definition for an enemy. Useful for accessing AI profile directly.
        /// </summary>
        public EnemyDefinition GetEnemyDefinition(int enemyId)
        {
            _enemyDefinitions.TryGetValue(enemyId, out var definition);
            return definition;
        }

        private EnemyData ConvertToEnemyData(EnemyDefinition definition)
        {
            // Convert ability definitions to runtime instances
            var abilities = definition.Abilities
                .Where(abilityDef => abilityDef != null)
                .Select(abilityDef => _abilityFactory.CreateAbilityInstance(abilityDef))
                .ToList();

            // Map AI profile to personality enum (fallback to SimpleRandom)
            var aiType = definition.AIProfile?.BasePersonality ?? AIPersonality.SimpleRandom;

            return new EnemyData
            {
                EnemyId = definition.EnemyId,
                Name = definition.Name,
                MaxHP = definition.MaxHP,
                Abilities = abilities,
                AIType = aiType,
                Prefab = definition.Prefab
            };
        }

        private EnemyData CreateDefaultEnemyData(int enemyId)
        {
            return new EnemyData
            {
                EnemyId = enemyId,
                Name = $"Unknown Enemy {enemyId}",
                MaxHP = 30,
                Abilities = new List<IAbilityInstance>(),
                AIType = AIPersonality.SimpleRandom
            };
        }
    }
}
