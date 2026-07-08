using Combat.Player.AI;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// The explicit SO → Core bridge for the global difficulty layer: snapshots a
    /// DifficultyDefinition into the pure AIDifficultySettings record at install time.
    /// A missing asset maps to Neutral (identity) so the game stays playable unconfigured.
    /// </summary>
    public static class DifficultyDefinitionMapper
    {
        public static AIDifficultySettings ToSettings(DifficultyDefinition definition)
        {
            if (definition == null)
                return AIDifficultySettings.Neutral;

            return new AIDifficultySettings(
                extraScoreNoise: definition.ExtraScoreNoise,
                extraTopN: definition.ExtraTopN,
                extraMistakeChance: definition.ExtraMistakeChance,
                aggressionScale: definition.AggressionScale,
                killSecuringScale: definition.KillSecuringScale,
                statusValueScale: definition.StatusValueScale);
        }
    }
}
