using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for managing available NPCs for binding to story templates.
    /// Tracks which NPCs are available, assigned, or on cooldown.
    /// </summary>
    public interface INpcPool
    {
        /// <summary>
        /// Gets all NPCs currently in the pool.
        /// </summary>
        IReadOnlyList<NpcInstance> AllNpcs { get; }

        /// <summary>
        /// Gets NPCs that are available for assignment.
        /// </summary>
        IReadOnlyList<NpcInstance> AvailableNpcs { get; }

        /// <summary>
        /// Populates the pool with NPCs from definitions.
        /// </summary>
        /// <param name="definitions">NPC definitions to instantiate</param>
        /// <param name="context">Narrative context for filtering</param>
        void PopulatePool(IReadOnlyList<NpcDefinition> definitions, INarrativeContext context);

        /// <summary>
        /// Clears all NPCs from the pool.
        /// </summary>
        void ClearPool();

        /// <summary>
        /// Reserves an NPC for a specific role in a story.
        /// </summary>
        /// <param name="npcId">The NPC instance ID to reserve</param>
        /// <param name="role">The role being assigned</param>
        /// <param name="storyId">The story this NPC is being assigned to</param>
        /// <returns>True if reservation was successful</returns>
        bool ReserveNpc(string npcId, NpcRole role, string storyId);

        /// <summary>
        /// Releases an NPC from their current assignment.
        /// </summary>
        /// <param name="npcId">The NPC instance ID to release</param>
        void ReleaseNpc(string npcId);

        /// <summary>
        /// Finds NPCs matching specified criteria.
        /// </summary>
        /// <param name="criteria">Search criteria for filtering</param>
        /// <returns>Matching NPCs ordered by suitability</returns>
        IReadOnlyList<NpcInstance> FindMatchingNpcs(NpcSearchCriteria criteria);

        /// <summary>
        /// Gets an NPC instance by ID.
        /// </summary>
        /// <param name="npcId">The NPC instance ID</param>
        /// <returns>The NPC instance or null if not found</returns>
        NpcInstance GetNpc(string npcId);

        /// <summary>
        /// Puts an NPC on cooldown after story completion.
        /// </summary>
        /// <param name="npcId">The NPC instance ID</param>
        /// <param name="cooldownPlatforms">Number of platforms before available again</param>
        void SetCooldown(string npcId, int cooldownPlatforms);

        /// <summary>
        /// Decrements cooldowns (called when platform changes).
        /// </summary>
        void DecrementCooldowns();
    }

    /// <summary>
    /// Role an NPC can play in a story.
    /// </summary>
    public enum NpcRole
    {
        None,
        QuestGiver,
        Companion,
        Antagonist,
        Merchant,
        Informant,
        Target,
        Bystander
    }

    /// <summary>
    /// Criteria for searching NPCs in the pool.
    /// </summary>
    public class NpcSearchCriteria
    {
        /// <summary>
        /// Required faction alignment.
        /// </summary>
        public NpcFaction? RequiredFaction { get; set; }

        /// <summary>
        /// Required role capability.
        /// </summary>
        public NpcRole? RequiredRole { get; set; }

        /// <summary>
        /// Required trait tags (any match).
        /// </summary>
        public List<string> RequiredTraits { get; set; } = new();

        /// <summary>
        /// Excluded NPC IDs.
        /// </summary>
        public List<string> ExcludedNpcIds { get; set; } = new();

        /// <summary>
        /// Whether to include NPCs on cooldown.
        /// </summary>
        public bool IncludeCooldown { get; set; }

        /// <summary>
        /// Whether to include assigned NPCs.
        /// </summary>
        public bool IncludeAssigned { get; set; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int MaxResults { get; set; } = 10;
    }
}
