namespace LevelGeneration.Journey
{
    /// <summary>
    /// The run's seeded biome itinerary: which biome (and escalation tier) is active for a given
    /// planning window. Pull-based single source of truth — queries are idempotent and
    /// order-independent, so the entrypoint (window 0 setup) and the streaming coordinator
    /// (per-window advance) can both consult it without a push/advance ordering hazard.
    /// </summary>
    public interface IBiomeJourney
    {
        /// <summary>The stretch covering <paramref name="windowIndex"/> (extends the plan lazily).</summary>
        BiomeStretch ForWindow(int windowIndex);
    }
}
