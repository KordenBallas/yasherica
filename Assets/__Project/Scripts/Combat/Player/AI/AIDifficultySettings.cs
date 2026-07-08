namespace Combat.Player.AI
{
    /// <summary>
    /// Immutable pure-C# snapshot of the global difficulty layer, mapped from
    /// DifficultyDefinition at install time. Modulates every enemy's AIBehaviorProfile at
    /// once: additive decision-quality degradation, multiplicative priority scaling.
    /// Deliberately carries NO stat multipliers — HP/damage scaling is Heat territory.
    /// </summary>
    public sealed class AIDifficultySettings
    {
        /// <summary>Identity modulation: effective tuning equals the profile unchanged.</summary>
        public static readonly AIDifficultySettings Neutral = new AIDifficultySettings();

        public AIDifficultySettings(
            float extraScoreNoise = 0f,
            int extraTopN = 0,
            float extraMistakeChance = 0f,
            float aggressionScale = 1f,
            float killSecuringScale = 1f,
            float statusValueScale = 1f)
        {
            ExtraScoreNoise = extraScoreNoise;
            ExtraTopN = extraTopN;
            ExtraMistakeChance = extraMistakeChance;
            AggressionScale = aggressionScale;
            KillSecuringScale = killSecuringScale;
            StatusValueScale = statusValueScale;
        }

        public float ExtraScoreNoise { get; }
        public int ExtraTopN { get; }
        public float ExtraMistakeChance { get; }
        public float AggressionScale { get; }
        public float KillSecuringScale { get; }
        public float StatusValueScale { get; }
    }
}
