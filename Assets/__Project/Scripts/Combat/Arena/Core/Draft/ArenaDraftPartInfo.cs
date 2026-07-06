namespace Combat.Arena.Core
{
    /// <summary>
    /// A part as the draft composer sees it: id + slot only. The SO → Core mapping happens at
    /// install time (ArenaDraftConfigMapper); Core never references PartDefinition.
    /// </summary>
    public class ArenaDraftPartInfo
    {
        public string PartId { get; }
        public string SlotId { get; }

        public ArenaDraftPartInfo(string partId, string slotId)
        {
            PartId = partId;
            SlotId = slotId;
        }
    }
}
