using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Provides filtered NPC queries and availability tracking.
    /// </summary>
    public interface INpcPool
    {
        /// <summary>
        /// Returns NPCs matching the given tags, excluding already-assigned NPCs.
        /// </summary>
        IReadOnlyList<NpcDefinition> Filter(
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> excludedNpcIds);

        /// <summary>
        /// Marks an NPC as assigned for this generation pass.
        /// </summary>
        void MarkAssigned(string npcId);

        /// <summary>
        /// Resets all assignment tracking for a new generation pass.
        /// </summary>
        void ResetAssignments();

        /// <summary>
        /// Total number of NPCs in the pool.
        /// </summary>
        int Count { get; }
    }
}
