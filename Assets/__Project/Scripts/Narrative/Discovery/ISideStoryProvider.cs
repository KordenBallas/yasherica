using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Discovery
{
    /// <summary>
    /// Context for side story selection.
    /// </summary>
    public class SideStorySelectionContext
    {
        /// <summary>
        /// Current story state.
        /// </summary>
        public StoryState StoryState { get; set; }

        /// <summary>
        /// Current chapter number (1-based).
        /// </summary>
        public int CurrentChapterNumber { get; set; }

        /// <summary>
        /// Optional tags to filter by.
        /// </summary>
        public List<string> FilterTags { get; set; }

        /// <summary>
        /// Optional NPC ID to filter by.
        /// </summary>
        public string FilterNpcId { get; set; }

        /// <summary>
        /// Number of side stories to select.
        /// </summary>
        public int MaxStoriesToSelect { get; set; } = 1;

        /// <summary>
        /// Exclude already completed side stories.
        /// </summary>
        public bool ExcludeCompleted { get; set; } = true;
    }

    /// <summary>
    /// Interface for providing filtered and weighted side stories.
    /// </summary>
    public interface ISideStoryProvider
    {
        /// <summary>
        /// Gets all available side stories that meet current prerequisites.
        /// </summary>
        /// <param name="context">Selection context with current state.</param>
        /// <returns>List of available side stories.</returns>
        IReadOnlyList<SideStoryDefinition> GetAvailableSideStories(SideStorySelectionContext context);

        /// <summary>
        /// Selects side stories using weighted random selection.
        /// </summary>
        /// <param name="context">Selection context with current state.</param>
        /// <returns>Selected side stories based on priority weights.</returns>
        IReadOnlyList<SideStoryDefinition> SelectWeightedRandomSideStories(SideStorySelectionContext context);

        /// <summary>
        /// Gets a side story by its ID.
        /// </summary>
        /// <param name="storyId">The story ID.</param>
        /// <returns>The side story definition, or null if not found.</returns>
        SideStoryDefinition GetSideStoryById(string storyId);

        /// <summary>
        /// Checks if a side story is on cooldown.
        /// </summary>
        /// <param name="storyId">The story ID to check.</param>
        /// <returns>True if on cooldown.</returns>
        bool IsOnCooldown(string storyId);

        /// <summary>
        /// Records that a side story was played (for cooldown tracking).
        /// </summary>
        /// <param name="storyId">The story ID that was played.</param>
        void RecordSideStoryPlayed(string storyId);

        /// <summary>
        /// Increments the platform counter (for cooldown calculation).
        /// </summary>
        void IncrementPlatformCounter();
    }
}
