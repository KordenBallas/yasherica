using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Discovery
{
    /// <summary>
    /// Interface for discovering side story content.
    /// Implementations auto-discover SideStoryDefinition assets.
    /// </summary>
    public interface ISideStoryDiscoveryService
    {
        /// <summary>
        /// Gets all discovered side story definitions.
        /// </summary>
        IReadOnlyList<SideStoryDefinition> AllSideStories { get; }

        /// <summary>
        /// Gets a side story by its unique ID.
        /// </summary>
        /// <param name="storyId">The story ID to look up.</param>
        /// <returns>The side story definition, or null if not found.</returns>
        SideStoryDefinition GetById(string storyId);

        /// <summary>
        /// Gets all side stories associated with a specific NPC.
        /// </summary>
        /// <param name="npcId">The NPC ID to filter by.</param>
        /// <returns>List of side stories for the NPC.</returns>
        IReadOnlyList<SideStoryDefinition> GetByNpc(string npcId);

        /// <summary>
        /// Gets all side stories with a specific tag.
        /// </summary>
        /// <param name="tag">The tag to filter by.</param>
        /// <returns>List of side stories with the tag.</returns>
        IReadOnlyList<SideStoryDefinition> GetByAttr(string tag);

        /// <summary>
        /// Gets all side stories with any of the specified tags.
        /// </summary>
        /// <param name="tags">Tags to filter by.</param>
        /// <returns>List of matching side stories.</returns>
        IReadOnlyList<SideStoryDefinition> GetByAttrs(IEnumerable<string> tags);

        /// <summary>
        /// Refreshes the discovery cache by reloading from Resources.
        /// </summary>
        void Refresh();
    }
}
