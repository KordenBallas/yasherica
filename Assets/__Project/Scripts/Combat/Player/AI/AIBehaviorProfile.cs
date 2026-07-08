namespace Combat.Player.AI
{
    /// <summary>
    /// Immutable pure-C# snapshot of one enemy's AI tuning, mapped from AIProfileDefinition
    /// at creation time (the SO → Core bridge). Defaults mirror the SO field initializers so
    /// an unconfigured enemy behaves like a profile-less one.
    /// </summary>
    public sealed class AIBehaviorProfile
    {
        public static readonly AIBehaviorProfile Default = new AIBehaviorProfile();

        public AIBehaviorProfile(
            int movementRange = 3,
            float damageWeight = 2.0f,
            float healWeight = 1.5f,
            float killBonus = 50f,
            float statusEffectBonus = 30f,
            float defensiveHpThreshold = 0.5f,
            float closeRangeBonus = 20f,
            float surroundPenalty = 15f,
            float scoreNoise = 0f,
            int pickFromTopN = 1,
            float mistakeChance = 0f,
            float focusWoundedWeight = 25f,
            float friendlyFirePenaltyWeight = 2f,
            float aggressionWeight = 1f,
            float selfPreservationWeight = 1f)
        {
            MovementRange = movementRange;
            DamageWeight = damageWeight;
            HealWeight = healWeight;
            KillBonus = killBonus;
            StatusEffectBonus = statusEffectBonus;
            DefensiveHpThreshold = defensiveHpThreshold;
            CloseRangeBonus = closeRangeBonus;
            SurroundPenalty = surroundPenalty;
            ScoreNoise = scoreNoise;
            PickFromTopN = pickFromTopN;
            MistakeChance = mistakeChance;
            FocusWoundedWeight = focusWoundedWeight;
            FriendlyFirePenaltyWeight = friendlyFirePenaltyWeight;
            AggressionWeight = aggressionWeight;
            SelfPreservationWeight = selfPreservationWeight;
        }

        public int MovementRange { get; }
        public float DamageWeight { get; }
        public float HealWeight { get; }
        public float KillBonus { get; }
        public float StatusEffectBonus { get; }
        public float DefensiveHpThreshold { get; }
        public float CloseRangeBonus { get; }
        public float SurroundPenalty { get; }
        public float ScoreNoise { get; }
        public int PickFromTopN { get; }
        public float MistakeChance { get; }
        public float FocusWoundedWeight { get; }
        public float FriendlyFirePenaltyWeight { get; }
        public float AggressionWeight { get; }
        public float SelfPreservationWeight { get; }
    }
}
