using MetaProgression.Core;

namespace MetaProgression.Data
{
    /// <summary>
    /// SO → Core bridge for the master tuning asset. A null SO (missing asset) maps to the Core
    /// defaults so the game always boots (FR14); the settings constructor re-clamps every value —
    /// in particular the bias ceiling stays strictly below 100% no matter what is authored (FR9).
    /// </summary>
    public static class MetaProgressionConfigMapper
    {
        public static MetaProgressionSettings ToSettings(MetaProgressionConfig config)
        {
            if (config == null)
            {
                return MetaProgressionSettings.Defaults;
            }

            return new MetaProgressionSettings(
                config.UnlockTierRunFloors,
                config.DirectionWindowRuns,
                config.RaceAxisWeight,
                config.ArtifactAxisWeight,
                config.BiasStrength,
                config.BiasCeiling,
                config.DigOfferSize,
                config.ReserveDirectionSlot,
                config.DilutionExponent,
                config.LedgerRetentionRuns);
        }
    }
}
