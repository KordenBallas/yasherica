namespace Combat.Core
{
    /// <summary>
    /// Which stat a modifier-type status (or a part's standing passive) moves.
    /// One flat signed magnitude per target — the shared modifier model used by both
    /// timed statuses and duration-less part passives (combat-status-effects FR10).
    /// </summary>
    public enum StatTarget
    {
        /// <summary>Flat delta added to damage the unit deals (Weakened −5 / Empowered +5).</summary>
        OutgoingDamage = 0,

        /// <summary>Flat delta added to damage the unit receives (Hardened −5).</summary>
        IncomingDamage = 1
    }
}
