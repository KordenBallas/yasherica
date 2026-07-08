using Combat.Player.AI;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// The explicit SO → Core bridge for AI profiles: snapshots an AIProfileDefinition into
    /// the pure AIBehaviorProfile record at decision-maker creation time, so the decision
    /// pipeline never touches Unity types.
    /// </summary>
    public static class AIProfileMapper
    {
        public static AIBehaviorProfile ToProfile(AIProfileDefinition definition)
        {
            if (definition == null)
                return AIBehaviorProfile.Default;

            return new AIBehaviorProfile(
                movementRange: definition.MovementRange,
                damageWeight: definition.DamageWeight,
                healWeight: definition.HealWeight,
                killBonus: definition.KillBonus,
                statusEffectBonus: definition.StatusEffectBonus,
                defensiveHpThreshold: definition.DefensiveHpThreshold,
                closeRangeBonus: definition.CloseRangeBonus,
                surroundPenalty: definition.SurroundPenalty,
                scoreNoise: definition.ScoreNoise,
                pickFromTopN: definition.PickFromTopN,
                mistakeChance: definition.MistakeChance,
                focusWoundedWeight: definition.FocusWoundedWeight,
                friendlyFirePenaltyWeight: definition.FriendlyFirePenaltyWeight,
                aggressionWeight: definition.AggressionWeight,
                selfPreservationWeight: definition.SelfPreservationWeight);
        }
    }
}
