using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Provides filtered story queries and cooldown tracking.
    /// </summary>
    public interface IStoryPool
    {
        /// <summary>
        /// Returns stories matching the given theme, difficulty, and tags.
        /// Excludes stories on cooldown.
        /// </summary>
        IReadOnlyList<StoryDefinition> Filter(
            LevelTheme theme,
            int difficulty,
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> excludedTags);

        /// <summary>
        /// Records that a story was used, starting its cooldown.
        /// </summary>
        void RecordUsage(string storyId);

        /// <summary>
        /// Advances cooldowns by one run.
        /// </summary>
        void TickCooldowns();

        /// <summary>
        /// Total number of stories in the pool.
        /// </summary>
        int Count { get; }
    }
}
