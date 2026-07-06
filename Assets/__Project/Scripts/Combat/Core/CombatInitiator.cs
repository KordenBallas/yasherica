namespace Combat.Core
{
    /// <summary>
    /// Who started a fight — decides who leads the opening round (the initiator acts first).
    /// A player-chosen attack leads with the player; an ambush or a dialogue that turned hostile
    /// leads with the enemy. Only the opening round is initiator-led; later rounds stay player-led
    /// (multi-round initiative policy is a deferred combat-design detail).
    /// </summary>
    public enum CombatInitiator
    {
        /// <summary>The player chose to attack (e.g. the encounter's Attack card).</summary>
        Player,

        /// <summary>An enemy caused the fight — an ambush/aggro cross, or an NPC that turned hostile mid-dialogue.</summary>
        Enemy
    }
}
