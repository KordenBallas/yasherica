namespace Combat.Arena.Core
{
    /// <summary>Outcome of applying a pick to the draft model (rejections are per G4 req 7).</summary>
    public enum ArenaDraftPickResult
    {
        Applied,
        NotYourTurn,
        EntryTaken,
        SlotAlreadyFilled,
        StalePickIndex,
        DraftComplete
    }
}
