namespace Narrative.Barks.Core
{
    /// <summary>
    /// The authored moments the cauldron's live voice fires at (P1-10, cauldron-voice-barks.md).
    /// This is the reactive bark channel — deliberately separate from the curated spine reveal
    /// beats (P3-1/P3-3), which are capped and gated, never fired live.
    /// </summary>
    public enum CauldronBarkSlot
    {
        /// <summary>A strong / monstrous mutation is on offer (the unseal menu surfaced a high-tier option).</summary>
        Temptation = 0,

        /// <summary>A dark offer or the attack verb is presented — the pull to Conquest, in fiction.</summary>
        DarkOffer = 1,

        /// <summary>A modest / marker (passport) part was taken — the sour counterpoint.</summary>
        Restraint = 2,

        /// <summary>A consistent socketing trend formed (via the ISocketingTrendSource seam).</summary>
        SocketingTrend = 3
    }
}
