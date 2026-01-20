using System.Collections.Generic;
using Narrative.Data;
using Narrative.Data.Definitions;

namespace Narrative.Graph
{
    /// <summary>
    /// Interface for story graph provider service.
    /// Provides query capabilities for the story graph.
    /// Follows Dependency Inversion Principle - consumers depend on this interface.
    /// </summary>
    public interface IStoryGraphProvider
    {
        /// <summary>
        /// Gets all available stories based on current story state.
        /// </summary>
        IReadOnlyList<BaseStoryDefinition> GetAvailableStories(StoryState currentState);

        /// <summary>
        /// Gets stories connected to a specific story via relationships.
        /// </summary>
        /// <param name="fromStory">Source story</param>
        /// <param name="filterType">Optional relationship type filter</param>
        IReadOnlyList<BaseStoryDefinition> GetConnectedStories(
            BaseStoryDefinition fromStory,
            StoryRelationshipType? filterType = null);

        /// <summary>
        /// Gets stories that match specific attributes.
        /// </summary>
        IReadOnlyList<BaseStoryDefinition> GetStoriesByAttributes(
            IReadOnlyList<StoryAttribute> attributes);

        /// <summary>
        /// Checks if a story is available based on current state.
        /// </summary>
        bool IsStoryAvailable(BaseStoryDefinition story, StoryState currentState);

        /// <summary>
        /// Gets stories on the critical path (main story progression).
        /// </summary>
        IReadOnlyList<BaseStoryDefinition> GetCriticalPath();

        /// <summary>
        /// Gets the next recommended story based on current state and context.
        /// </summary>
        BaseStoryDefinition GetNextStory(StoryState currentState, StorySelectionContext context);

        /// <summary>
        /// Marks a story as completed and updates the graph.
        /// </summary>
        void CompleteStory(string storyId);

        /// <summary>
        /// Refreshes the graph (rebuilds from story definitions).
        /// </summary>
        void RefreshGraph();

        /// <summary>
        /// Gets statistics about the current graph state.
        /// </summary>
        GraphStatistics GetStatistics();

        /// <summary>
        /// Gets the story graph instance (for advanced queries).
        /// </summary>
        StoryGraph GetGraph();
    }

    /// <summary>
    /// Context for story selection.
    /// Provides additional information to help select the next story.
    /// </summary>
    public class StorySelectionContext
    {
        /// <summary>
        /// Recently completed stories (for finding connected stories).
        /// </summary>
        public List<string> RecentlyCompletedStories { get; set; } = new();

        /// <summary>
        /// Accumulated story attributes from completed stories.
        /// </summary>
        public StoryAttributeCollection AccumulatedAttributes { get; set; } = new();

        /// <summary>
        /// Current chapter number.
        /// </summary>
        public int CurrentChapterNumber { get; set; }

        /// <summary>
        /// Preferred story type (if any).
        /// </summary>
        public StoryType? PreferredStoryType { get; set; }

        /// <summary>
        /// Maximum number of stories to return.
        /// </summary>
        public int MaxStories { get; set; } = 5;

        /// <summary>
        /// Whether to prioritize connected stories.
        /// </summary>
        public bool PrioritizeConnectedStories { get; set; } = true;
    }
}
