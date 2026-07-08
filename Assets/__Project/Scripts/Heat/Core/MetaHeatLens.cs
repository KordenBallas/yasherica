using MetaProgression.Core;

namespace Heat.Core
{
    /// <summary>
    /// Heat's implementation of R's vocabulary seam (<see cref="IHeatLens"/>): projects the live
    /// <see cref="IHeatLevels"/> readings plus the settings' floor-relief curve. Delegates per read,
    /// so a Hub lens over the staged pact stays current as the player cycles ranks at the cauldron.
    /// </summary>
    public sealed class MetaHeatLens : IHeatLens
    {
        private readonly HeatSettings _settings;
        private readonly IHeatLevels _levels;

        public MetaHeatLens(HeatSettings settings, IHeatLevels levels)
        {
            _settings = settings ?? HeatSettings.Defaults;
            _levels = levels;
        }

        public int CurrentHeat => _levels?.CurrentTotalHeat ?? 0;

        public int HighWaterHeat => _levels?.HighWaterHeat ?? 0;

        public int FloorReliefRuns => _settings.FloorReliefFor(CurrentHeat);
    }
}
