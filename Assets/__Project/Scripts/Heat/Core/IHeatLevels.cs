namespace Heat.Core
{
    /// <summary>
    /// The two Heat readings a min-Heat gate can key off (heat-ascension FR5): the pact currently in
    /// force (the run being played, or in the Hub the pact being staged live at the cauldron) and
    /// the persisted hottest clear. Kept behind an interface because the Hub's "current" is a live
    /// model while the Area's is a fixed run-scoped value.
    /// </summary>
    public interface IHeatLevels
    {
        int CurrentTotalHeat { get; }
        int HighWaterHeat { get; }
    }
}
