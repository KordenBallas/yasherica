using System.Collections.Generic;
using Narrative.Data;
using Narrative.Data.Definitions;

namespace Narrative.Providers
{
    /// <summary>
    /// Interface for NPC story provider service.
    /// Provides query capabilities for NPC-associated stories.
    /// Follows Dependency Inversion Principle - consumers depend on this interface.
    /// </summary>
    public interface INpcStoryProvider
    {
        /// <summary>
        /// Gets all stories associated with an NPC.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <returns>All stories associated with this NPC</returns>
        IReadOnlyList<BaseStoryDefinition> GetStoriesForNpc(string npcId);

        /// <summary>
        /// Gets stories associated with an NPC that are currently available.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="currentState">Current story state</param>
        /// <returns>Available stories for this NPC</returns>
        IReadOnlyList<BaseStoryDefinition> GetAvailableStoriesForNpc(string npcId, StoryState currentState);

        /// <summary>
        /// Gets the next recommended story for an NPC encounter.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="currentState">Current story state</param>
        /// <returns>Next story to play for this NPC, or null if none available</returns>
        BaseStoryDefinition GetNextStoryForNpc(string npcId, StoryState currentState);

        /// <summary>
        /// Gets stories where the NPC has a specific role.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="role">Role to filter by</param>
        /// <returns>Stories where NPC has this role</returns>
        IReadOnlyList<BaseStoryDefinition> GetStoriesByRole(string npcId, NpcStoryRole role);

        /// <summary>
        /// Checks if an NPC has any available stories.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="currentState">Current story state</param>
        /// <returns>True if NPC has at least one available story</returns>
        bool HasAvailableStories(string npcId, StoryState currentState);

        /// <summary>
        /// Gets the NPC definition by ID.
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <returns>NPC definition, or null if not found</returns>
        NpcDefinition GetNpcDefinition(string npcId);

        /// <summary>
        /// Gets all NPCs that have stories associated with them.
        /// </summary>
        /// <returns>All NPCs with story associations</returns>
        IReadOnlyList<NpcDefinition> GetAllStoryNpcs();
    }
}
