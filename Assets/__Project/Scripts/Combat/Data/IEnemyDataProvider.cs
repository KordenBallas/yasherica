using System.Collections.Generic;
using Combat.Core;
using UnityEngine;

namespace Combat.Data
{
    /// <summary>
    /// Provides enemy configuration data based on enemy ID.
    /// Follows Dependency Inversion Principle - depend on abstraction, not implementation.
    /// </summary>
    public interface IEnemyDataProvider
    {
        /// <summary>
        /// Retrieves enemy data for a given enemy ID.
        /// </summary>
        /// <param name="enemyId">The unique identifier for the enemy</param>
        /// <returns>Enemy data including stats, abilities, and AI configuration</returns>
        EnemyData GetEnemyData(int enemyId);
    }

    /// <summary>
    /// Data transfer object containing enemy configuration.
    /// Pure data class with no behavior.
    /// </summary>
    public class EnemyData
    {
        public int EnemyId { get; set; }
        public string Name { get; set; }
        public int MaxHP { get; set; }
        public List<IAbilityInstance> Abilities { get; set; }
        public AIPersonality AIType { get; set; }
        public GameObject Prefab { get; set; }

        /// <summary>
        /// Artifact drop slots rolled on defeat; empty falls back to the
        /// biome enemy drop table.
        /// </summary>
        public IReadOnlyList<Loot.Core.LootSlotData> LootSlots { get; set; }
    }

    /// <summary>
    /// Enum defining available AI decision-making strategies.
    /// </summary>
    public enum AIPersonality
    {
        /// <summary>
        /// Picks random valid actions from available options.
        /// </summary>
        SimpleRandom,

        /// <summary>
        /// Evaluates all actions and picks the highest scoring option.
        /// </summary>
        Tactical
    }
}
