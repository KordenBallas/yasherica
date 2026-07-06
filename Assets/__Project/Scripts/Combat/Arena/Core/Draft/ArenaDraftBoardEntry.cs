namespace Combat.Arena.Core
{
    /// <summary>
    /// One draftable instance on the shared board. Entries are instances, not unique part ids:
    /// the common floor may stock several copies of the same part so every seat can always fill
    /// every slot (P4-5 req 6), while catalog-sampled entries are single-copy — denial removes
    /// the entry, not the part id.
    /// </summary>
    public class ArenaDraftBoardEntry
    {
        public int EntryId { get; }
        public string PartId { get; }
        public string SlotId { get; }

        public ArenaDraftBoardEntry(int entryId, string partId, string slotId)
        {
            EntryId = entryId;
            PartId = partId;
            SlotId = slotId;
        }
    }
}
