namespace Combat.Arena.View
{
    /// <summary>What a click on the draft stage landed on: a board entry or a hero part.</summary>
    public readonly struct ArenaDraftStageHit
    {
        public bool IsBoardEntry => EntryId >= 0;
        public bool IsHeroPart => !IsBoardEntry && !string.IsNullOrEmpty(HeroSlotId);

        public int EntryId { get; }
        public string HeroSlotId { get; }

        public static ArenaDraftStageHit Entry(int entryId) => new ArenaDraftStageHit(entryId, null);
        public static ArenaDraftStageHit HeroPart(string slotId) => new ArenaDraftStageHit(-1, slotId);

        private ArenaDraftStageHit(int entryId, string heroSlotId)
        {
            EntryId = entryId;
            HeroSlotId = heroSlotId;
        }
    }
}
