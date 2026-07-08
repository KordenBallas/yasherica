namespace Combat.Player.AI
{
    /// <summary>
    /// Where the AI factory reads the current global difficulty from. Today a static
    /// install-time snapshot; a future settings screen swaps in a persisted-selection
    /// source behind this same seam.
    /// </summary>
    public interface IAIDifficultySource
    {
        AIDifficultySettings Current { get; }
    }
}
