using Heat.Core;

namespace Hub.Core
{
    /// <summary>
    /// The Hub's Heat readings (heat-ascension FR5/FR6): "current" is the LIVE pact being staged at
    /// the cauldron — cranking a rank immediately changes what the dig and the min-Heat gates
    /// answer, which is the point (the pact of the run being launched IS the current pact here);
    /// the high-water mark is fixed as persisted when the scene booted.
    /// </summary>
    public sealed class HubHeatLevels : IHeatLevels
    {
        private readonly HubHeatModel _model;
        private readonly int _highWaterHeat;

        public HubHeatLevels(HubHeatModel model, int highWaterHeat)
        {
            _model = model;
            _highWaterHeat = highWaterHeat < 0 ? 0 : highWaterHeat;
        }

        public int CurrentTotalHeat => _model?.TotalHeat ?? 0;

        public int HighWaterHeat => _highWaterHeat;
    }
}
