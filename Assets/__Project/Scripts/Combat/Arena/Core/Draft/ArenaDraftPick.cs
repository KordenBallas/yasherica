namespace Combat.Arena.Core
{
    /// <summary>
    /// One draft action: at PickIndex, PlayerId takes board entry EntryId. WasAutoPick marks
    /// host-issued fills (timeout / AI seat / departed seat) so the screen can present them
    /// differently; it never changes validation.
    /// </summary>
    public class ArenaDraftPick
    {
        public int PickIndex { get; }
        public int PlayerId { get; }
        public int EntryId { get; }
        public bool WasAutoPick { get; }

        public ArenaDraftPick(int pickIndex, int playerId, int entryId, bool wasAutoPick)
        {
            PickIndex = pickIndex;
            PlayerId = playerId;
            EntryId = entryId;
            WasAutoPick = wasAutoPick;
        }
    }
}
