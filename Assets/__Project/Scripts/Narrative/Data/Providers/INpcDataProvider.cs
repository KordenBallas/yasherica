using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Data.Providers
{
    /// <summary>
    /// Interface for retrieving NPC definitions.
    /// </summary>
    public interface INpcDataProvider
    {
        /// <summary>
        /// Gets an NPC definition by ID.
        /// </summary>
        NpcDefinition GetNpcById(string npcId);

        /// <summary>
        /// Gets all available NPC definitions.
        /// </summary>
        IReadOnlyList<NpcDefinition> GetAllNpcs();

        /// <summary>
        /// Gets NPCs by faction.
        /// </summary>
        IReadOnlyList<NpcDefinition> GetNpcsByFaction(NpcFaction faction);

        /// <summary>
        /// Checks if an NPC with the given ID exists.
        /// </summary>
        bool HasNpc(string npcId);
    }
}
