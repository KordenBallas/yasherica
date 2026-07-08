namespace Heat.Core
{
    /// <summary>
    /// The Area's Heat readings (heat-ascension FR2): the run's pact is sealed at launch, so both
    /// values are fixed for the scene — current = the sealed pact's total, high-water = the record
    /// as persisted when the scene booted (a record set THIS run only matters to later scenes).
    /// </summary>
    public sealed class AreaHeatLevels : IHeatLevels
    {
        public AreaHeatLevels(int currentTotalHeat, int highWaterHeat)
        {
            CurrentTotalHeat = currentTotalHeat < 0 ? 0 : currentTotalHeat;
            HighWaterHeat = highWaterHeat < 0 ? 0 : highWaterHeat;
        }

        public int CurrentTotalHeat { get; }

        public int HighWaterHeat { get; }
    }
}
