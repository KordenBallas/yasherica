namespace Combat.Arena.Core
{
    /// <summary>Which <see cref="IArenaResolutionOrder"/> strategy the match resolves with (P4-3a).</summary>
    public enum ArenaResolutionOrderMode
    {
        /// <summary>Round N starts at (N−1) mod aliveCount of the PlayerId-sorted commits — the shipped default.</summary>
        RotatingInitiative,

        /// <summary>Per-round seeded Fisher–Yates shuffle — unpredictable but identical on every client.</summary>
        SeededShuffle
    }
}
