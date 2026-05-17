using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for providing story policies based on story type.
    /// Enables different behavioral configurations for Main Story vs Side Story.
    /// </summary>
    public interface IStoryPolicyProvider
    {
        /// <summary>
        /// Gets the policy for the specified story type.
        /// </summary>
        /// <param name="storyType">The type of story to get policy for</param>
        /// <returns>The policy configuration for the story type</returns>
        StoryPolicy GetPolicy(StoryType storyType);

        /// <summary>
        /// Gets the policy for main story (Chapter) type.
        /// </summary>
        StoryPolicy MainStoryPolicy { get; }

        /// <summary>
        /// Gets the policy for side story type.
        /// </summary>
        StoryPolicy SideStoryPolicy { get; }

        /// <summary>
        /// Calculates the number of side stories for an area based on platform count.
        /// Formula: max(0, (TotalPlatforms / 3) - 1)
        /// </summary>
        /// <param name="totalPlatforms">Total platforms in the area</param>
        /// <returns>Number of side stories to generate</returns>
        int CalculateSideStoryCount(int totalPlatforms);
    }
}
