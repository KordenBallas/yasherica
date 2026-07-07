namespace Combat.Player
{
    /// <summary>
    /// One active status on one unit, as the on-board status row shows it:
    /// which glyph (by status id), how many turns remain (-1 = permanent, e.g. a part
    /// passive — shown without a number), and the stack count (shown as ×N when > 1).
    /// </summary>
    public readonly struct StatusIconModel
    {
        public int UnitId { get; }
        public int StatusId { get; }
        public int RemainingTurns { get; }
        public int StackCount { get; }

        public StatusIconModel(int unitId, int statusId, int remainingTurns, int stackCount)
        {
            UnitId = unitId;
            StatusId = statusId;
            RemainingTurns = remainingTurns;
            StackCount = stackCount;
        }
    }
}
