namespace Combat.Core
{
    /// <summary>
    /// Decides who leads a round given the round number and the fight's initiator (D2). Only the
    /// <b>opening</b> round is initiator-led: an enemy-initiated fight resolves the committed enemy
    /// intents before the player's Act phase. Later rounds stay player-led (the multi-round initiative
    /// policy — alternate / persist / re-roll — is a deferred combat-design detail, per the brief).
    /// Pure and deterministic: same round number + same initiator → same lead.
    /// </summary>
    public static class RoundLeadPolicy
    {
        /// <summary>The first round (turn number 1); only it is initiator-led.</summary>
        private const int OpeningRoundNumber = 1;

        /// <summary>
        /// True when the enemy side should resolve before the player acts this round — i.e. the opening
        /// round of an enemy-initiated fight.
        /// </summary>
        public static bool EnemyLeadsThisRound(int turnNumber, CombatInitiator initiator)
        {
            return turnNumber == OpeningRoundNumber && initiator == CombatInitiator.Enemy;
        }
    }
}
