namespace Combat.Core
{
    /// <summary>
    /// Defines the type of win condition.
    /// </summary>
    public enum WinConditionType
    {
        /// <summary>
        /// Win by eliminating all enemy units.
        /// </summary>
        EliminateAllEnemies,
        
        /// <summary>
        /// Win by reaching a specific objective.
        /// </summary>
        ReachObjective,
        
        /// <summary>
        /// Win by surviving a certain number of turns.
        /// </summary>
        SurviveTurns,
        
        /// <summary>
        /// Win by protecting a VIP unit.
        /// </summary>
        ProtectUnit
    }
}

