using System;

namespace Combat.Player.AI
{
    /// <summary>
    /// The composed, effective dials the decision pipeline reads: a per-enemy
    /// AIBehaviorProfile modulated by the global AIDifficultySettings. Composing with
    /// AIDifficultySettings.Neutral reproduces the profile unchanged (back-compat).
    /// </summary>
    public sealed class AITuning
    {
        /// <summary>Discount on next-round aim potential when scoring a move destination.</summary>
        public const float LookaheadDiscount = 0.6f;

        /// <summary>Score of doing nothing; abilities that hit no one must lose to this.</summary>
        public const float EndTurnBaselineScore = 10f;

        /// <summary>Deterministic cap on evaluated move destinations per decision.</summary>
        public const int MaxEvaluatedMovePositions = 64;

        private AITuning(
            int movementRange,
            float damageWeight,
            float healWeight,
            float killBonus,
            float statusEffectBonus,
            float defensiveHpThreshold,
            float closeRangeBonus,
            float surroundPenalty,
            float focusWoundedWeight,
            float friendlyFirePenaltyWeight,
            float aggressionWeight,
            float selfPreservationWeight,
            float scoreNoise,
            int pickFromTopN,
            float mistakeChance)
        {
            MovementRange = movementRange;
            DamageWeight = damageWeight;
            HealWeight = healWeight;
            KillBonus = killBonus;
            StatusEffectBonus = statusEffectBonus;
            DefensiveHpThreshold = defensiveHpThreshold;
            CloseRangeBonus = closeRangeBonus;
            SurroundPenalty = surroundPenalty;
            FocusWoundedWeight = focusWoundedWeight;
            FriendlyFirePenaltyWeight = friendlyFirePenaltyWeight;
            AggressionWeight = aggressionWeight;
            SelfPreservationWeight = selfPreservationWeight;
            ScoreNoise = scoreNoise;
            PickFromTopN = pickFromTopN;
            MistakeChance = mistakeChance;
        }

        public int MovementRange { get; }
        public float DamageWeight { get; }
        public float HealWeight { get; }
        public float KillBonus { get; }
        public float StatusEffectBonus { get; }
        public float DefensiveHpThreshold { get; }
        public float CloseRangeBonus { get; }
        public float SurroundPenalty { get; }
        public float FocusWoundedWeight { get; }
        public float FriendlyFirePenaltyWeight { get; }
        public float AggressionWeight { get; }
        public float SelfPreservationWeight { get; }
        public float ScoreNoise { get; }
        public int PickFromTopN { get; }
        public float MistakeChance { get; }

        public static AITuning Compose(AIBehaviorProfile profile, AIDifficultySettings difficulty)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (difficulty == null) throw new ArgumentNullException(nameof(difficulty));

            return new AITuning(
                movementRange: profile.MovementRange,
                damageWeight: profile.DamageWeight,
                healWeight: profile.HealWeight,
                killBonus: profile.KillBonus * difficulty.KillSecuringScale,
                statusEffectBonus: profile.StatusEffectBonus * difficulty.StatusValueScale,
                defensiveHpThreshold: profile.DefensiveHpThreshold,
                closeRangeBonus: profile.CloseRangeBonus,
                surroundPenalty: profile.SurroundPenalty,
                focusWoundedWeight: profile.FocusWoundedWeight,
                friendlyFirePenaltyWeight: profile.FriendlyFirePenaltyWeight,
                aggressionWeight: profile.AggressionWeight * difficulty.AggressionScale,
                selfPreservationWeight: profile.SelfPreservationWeight,
                scoreNoise: profile.ScoreNoise + difficulty.ExtraScoreNoise,
                pickFromTopN: Math.Max(1, profile.PickFromTopN + difficulty.ExtraTopN),
                mistakeChance: Math.Min(1f, Math.Max(0f, profile.MistakeChance + difficulty.ExtraMistakeChance)));
        }
    }
}
