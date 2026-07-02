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

        public PartBlankData(
            string definitionId,
            string displayName,
            string slotId,
            string speciesArchetypeId,
            int socketCount)
        {
            DefinitionId = definitionId;
            DisplayName = displayName;
            SlotId = slotId;
            SpeciesArchetypeId = speciesArchetypeId;
            SocketCount = socketCount < 1 ? 1 : socketCount;
        }
    }
}
