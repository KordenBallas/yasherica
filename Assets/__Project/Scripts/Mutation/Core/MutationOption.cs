namespace Mutation.Core
{
    /// <summary>
    /// One offered stage-up mutation: swapping the body part in <see cref="SlotId"/> for the part
    /// <see cref="PartId"/>. <see cref="ArchetypeId"/> is the part's dominant-affinity archetype,
    /// used only to tint the choice button. UnityEngine-free; the presenter resolves the icon/tint at
    /// the view boundary so Core never holds Unity types. Built by <see cref="IMutationOptionBuilder"/>
    /// by scoring every candidate part against the cumulative feed tally.
    /// </summary>
    public sealed class MutationOption
    {
        public string SlotId { get; }
        public string PartId { get; }
        public string ArchetypeId { get; }
        public string DisplayName { get; }

        public MutationOption(string slotId, string partId, string archetypeId, string displayName)
        {
            SlotId = slotId;
            PartId = partId;
            ArchetypeId = archetypeId;
            DisplayName = displayName;
        }
    }
}
