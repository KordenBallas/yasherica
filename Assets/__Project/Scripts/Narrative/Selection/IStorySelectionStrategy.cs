using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;
using Narrative.Graph;

namespace Narrative.Selection
{
    /// <summary>
    /// Interface for story selection strategies.
    /// Strategies determine which stories to include in a scenario based on different approaches.
    /// Follows Strategy pattern - allows swapping selection algorithms at runtime.
    /// </summary>
    public interface IStorySelectionStrategy
    {
        /// <summary>
        /// Selects stories from available options based on strategy rules.
        /// </summary>
        /// <param name="availableStories">All stories that are currently available</param>
        /// <param name="context">Game context for contextual decisions</param>
        /// <param name="graph">Story graph for querying relationships</param>
        /// <param name="maxStories">Maximum number of stories to select</param>
        /// <returns>Selected and prioritized stories</returns>
        IReadOnlyList<BaseStoryDefinition> SelectStories(
            IReadOnlyList<BaseStoryDefinition> availableStories,
            GameContext context,
            IStoryGraphProvider graph,
            int maxStories);

        /// <summary>
        /// Name of this strategy (for debugging and UI).
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// Description of what this strategy prioritizes.
        /// </summary>
        string Description { get; }
    }
}
