using MetaProgression.Core;

namespace Mutation.Core
{
    /// <summary>
    /// One authored Part-Blank in UnityEngine-free terms: the organ it grows
    /// (the character slot), the species/passport marker it carries (an archetype
    /// id - the blank, not the reagents, decides what races read), and how many
    /// artifact sockets it exposes. Built by the Data layer from a
    /// <c>PartBlankDefinition</c>.
    /// </summary>
    public sealed class PartBlankData
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string SlotId { get; }
        public string SpeciesArchetypeId { get; }
        public int SocketCount { get; }

        /// <summary>Race tag for the quest-reward economy (empty = kindless), distinct from the
        /// species archetype until the species-vs-race reconcile (Track J) lands.</summary>
        public string RaceId { get; }

        /// <summary>Meta-progression gate (Track R); rides the pure record so draw-time consumers
        /// (quest-reward pools) can consult the vocabulary without touching the SO. Never null.</summary>
        public MetaGate Gate { get; }

        public PartBlankData(
            string definitionId,
            string displayName,
            string slotId,
            string speciesArchetypeId,
            int socketCount,
            string raceId = "",
            MetaGate gate = null)
        {
            DefinitionId = definitionId;
            DisplayName = displayName;
            SlotId = slotId;
            SpeciesArchetypeId = speciesArchetypeId;
            SocketCount = socketCount < 1 ? 1 : socketCount;
            RaceId = raceId ?? string.Empty;
            Gate = gate ?? MetaGate.Base;
        }
    }
}
