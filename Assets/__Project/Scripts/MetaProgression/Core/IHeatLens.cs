namespace MetaProgression.Core
{
    /// <summary>
    /// Everything the vocabulary needs to know about Heat (heat-ascension FR5/FR6), kept as a narrow
    /// seam on R's side so the dependency points one way (Heat → MetaProgression, never back).
    /// Absent (null) = Heat not installed: no floor relief, and min-Heat gates fail closed.
    /// Readings may be live — the Hub's lens follows the pact being staged at the cauldron.
    /// </summary>
    public interface IHeatLens
    {
        /// <summary>Total Heat of the pact in force (Area: the sealed run pact; Hub: the pact being staged).</summary>
        int CurrentHeat { get; }

        /// <summary>The persisted hottest cleared total Heat.</summary>
        int HighWaterHeat { get; }

        /// <summary>Runs subtracted from every gated token's effective tier run-floor at the current Heat.</summary>
        int FloorReliefRuns { get; }
    }
}
