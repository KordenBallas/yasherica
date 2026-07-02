namespace Mutation.Core
{
    /// <summary>
    /// One offered unseal variant: swapping the body part in <see cref="SlotId"/> for the part
    /// <see cref="PartId"/>. <see cref="ArchetypeId"/> is the blank's species/passport archetype,
    /// used only to tint the variant card. UnityEngine-free; the presenter resolves the icon/tint at
    /// the view boundary so Core never holds Unity types. Built by <see cref="IBlankVariantBuilder"/>
    /// by scoring the candidate parts of the blank's slot against the socketed reagents.
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
