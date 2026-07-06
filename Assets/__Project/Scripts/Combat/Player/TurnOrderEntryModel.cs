namespace Combat.Player
{
    /// <summary>
    /// One actor in the turn-order strip (D2): a unit that acts this round, in resolution order.
    /// Pure model — the view resolves the visuals (label + current/acted tint). <see cref="IsCurrent"/>
    /// marks the side acting right now; <see cref="HasActed"/> marks a side that already resolved its
    /// phase this round. Dead units are omitted by the presenter, so every entry is a live actor.
    /// </summary>
    public readonly struct TurnOrderEntryModel
    {
        public int UnitId { get; }
        public string Name { get; }
        public bool IsPlayer { get; }
        public bool IsCurrent { get; }
        public bool HasActed { get; }

        public TurnOrderEntryModel(int unitId, string name, bool isPlayer, bool isCurrent, bool hasActed)
        {
            UnitId = unitId;
            Name = name;
            IsPlayer = isPlayer;
            IsCurrent = isCurrent;
            HasActed = hasActed;
        }
    }
}
