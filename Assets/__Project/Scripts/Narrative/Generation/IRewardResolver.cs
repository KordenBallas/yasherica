using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Resolves rewards from a story's RewardSlots into concrete ResolvedRewards.
    /// Handles probability rolls and quantity randomization.
    /// </summary>
    public interface IRewardResolver
    {
        /// <summary>
        /// Resolves rewards for a story based on its reward slots.
        /// </summary>
        IReadOnlyList<ResolvedReward> Resolve(StoryDefinition story);
    }
}
