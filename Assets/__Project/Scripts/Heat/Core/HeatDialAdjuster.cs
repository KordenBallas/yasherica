using MetaProgression.Core;

namespace Heat.Core
{
    /// <summary>
    /// Maps total Heat onto R's dig dials (heat-ascension FR6): lifts <c>BiasStrength</c> and may
    /// enable the reserve-direction slot. The bias ceiling is deliberately not a parameter — Heat
    /// raises odds and the floor, never the cap; re-running the <see cref="MetaProgressionSettings"/>
    /// constructor keeps the never-guarantee clamp in force whatever the lift.
    /// </summary>
    public static class HeatDialAdjuster
    {
        public static MetaProgressionSettings Apply(MetaProgressionSettings baseSettings, HeatSettings heat, int totalHeat)
        {
            if (baseSettings == null)
            {
                baseSettings = MetaProgressionSettings.Defaults;
            }

            if (heat == null || totalHeat <= 0)
            {
                return baseSettings;
            }

            bool reserveSlot = baseSettings.ReserveDirectionSlot
                || (heat.ReserveDirectionSlotMinHeat > 0 && totalHeat >= heat.ReserveDirectionSlotMinHeat);
            return new MetaProgressionSettings(
                unlockTierRunFloors: baseSettings.UnlockTierRunFloors,
                directionWindowRuns: baseSettings.DirectionWindowRuns,
                raceAxisWeight: baseSettings.RaceAxisWeight,
                artifactAxisWeight: baseSettings.ArtifactAxisWeight,
                biasStrength: baseSettings.BiasStrength + heat.BiasStrengthLiftPerHeat * totalHeat,
                biasCeiling: baseSettings.BiasCeiling,
                digOfferSize: baseSettings.DigOfferSize,
                reserveDirectionSlot: reserveSlot,
                dilutionExponent: baseSettings.DilutionExponent,
                ledgerRetentionRuns: baseSettings.LedgerRetentionRuns);
        }
    }
}
