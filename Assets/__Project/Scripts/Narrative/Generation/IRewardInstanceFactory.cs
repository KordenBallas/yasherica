using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Factory interface for creating reward instances from definitions.
    /// Handles chapter scaling, multipliers, and batch creation from template slots.
    /// </summary>
    public interface IRewardInstanceFactory
    {
        /// <summary>
        /// Creates a single reward instance from a definition.
        /// </summary>
        /// <param name="definition">The reward definition</param>
        /// <param name="chapterNumber">Current chapter for scaling</param>
        /// <param name="multiplier">Value multiplier to apply</param>
        /// <param name="condition">Condition for this reward</param>
        /// <returns>A new reward instance</returns>
        RewardInstance Create(
            RewardDefinition definition,
            int chapterNumber,
            float multiplier = 1f,
            RewardCondition condition = RewardCondition.Always);

        /// <summary>
        /// Creates reward instances from template reward slots.
        /// Applies probability rolls and filters by chapter requirements.
        /// </summary>
        /// <param name="slots">Template reward slots to process</param>
        /// <param name="chapterNumber">Current chapter for scaling and filtering</param>
        /// <returns>List of created reward instances</returns>
        IReadOnlyList<RewardInstance> CreateFromSlots(
            IReadOnlyList<TemplateRewardSlot> slots,
            int chapterNumber);

        /// <summary>
        /// Creates reward instances from template reward slots with a custom random seed.
        /// Useful for deterministic reward generation.
        /// </summary>
        /// <param name="slots">Template reward slots to process</param>
        /// <param name="chapterNumber">Current chapter for scaling and filtering</param>
        /// <param name="seed">Random seed for deterministic generation</param>
        /// <returns>List of created reward instances</returns>
        IReadOnlyList<RewardInstance> CreateFromSlots(
            IReadOnlyList<TemplateRewardSlot> slots,
            int chapterNumber,
            int seed);

        /// <summary>
        /// Restores a reward instance from a snapshot.
        /// Used during save/load operations.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from</param>
        /// <param name="definition">The reward definition</param>
        /// <returns>Restored reward instance, or null if restoration failed</returns>
        RewardInstance RestoreFromSnapshot(
            RewardInstanceSnapshot snapshot,
            RewardDefinition definition);
    }
}
