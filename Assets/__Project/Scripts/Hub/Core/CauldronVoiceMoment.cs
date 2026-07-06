namespace Hub.Core
{
    /// <summary>
    /// The staging moments the cauldron's voice reacts to on the Hub (O1). A deliberately small
    /// Hub-only surface — the full in-run bark channel (P1-10, slot × path-lean) will absorb these
    /// as its hub-presence slots when it lands.
    /// </summary>
    public enum CauldronVoiceMoment
    {
        /// <summary>The player picked a starting organ (race-keyed pools apply).</summary>
        PartPicked = 0,

        /// <summary>The offer is empty — the first, bare launch.</summary>
        NoPartAvailable = 1,

        /// <summary>The launch/descent fires.</summary>
        Launch = 2,

        /// <summary>The player just returned from a death (the junkyard reformed him).</summary>
        DeathReturn = 3
    }
}
