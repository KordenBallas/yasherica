using System.Collections.Generic;
using Combat.Core;

namespace Combat.Data
{
    /// <summary>
    /// Simple implementation of IEnemyDataProvider with hardcoded enemy data.
    /// For MVP/prototyping - replace with ScriptableObject-based system later.
    /// Pure C# class following SOLID principles.
    /// </summary>
    public class SimpleEnemyDataProvider : IEnemyDataProvider
    {
        /// <summary>
        /// Returns enemy data for the given enemy ID.
        /// Currently returns same stats for all IDs (MVP implementation).
        /// </summary>
        public EnemyData GetEnemyData(int enemyId)
        {
            return new EnemyData
            {
                EnemyId = enemyId,
                Name = $"Enemy {enemyId}",
                MaxHP = 50,
                Abilities = CreateDefaultAbilities(),
                AIType = AIPersonality.SimpleRandom,
                Prefab = null  // No prefab - will fallback to Resources loading
            };
        }

        /// <summary>
        /// Creates default ability set for enemies.
        /// </summary>
        private List<IAbilityInstance> CreateDefaultAbilities()
        {
            return new List<IAbilityInstance>
            {
                new AbilityInstance(new MeleeAttackAbility(10)),  // Basic attack, no cooldown
                new AbilityInstance(new PowerAttackAbility(20))    // Strong attack, 2 turn cooldown
            };
        }
    }
}
