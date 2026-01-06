namespace Combat.Core
{
    /// <summary>
    /// Represents the current phase of the game.
    /// </summary>
    public enum CombatPhase
    {
        /// <summary>
        /// Game is being set up (placing units, configuring).
        /// </summary>
        Setup,
        
        /// <summary>
        /// Active combat phase.
        /// </summary>
        Combat,
        
        /// <summary>
        /// Game has been won.
        /// </summary>
        Victory,
        
        /// <summary>
        /// Game has been lost.
        /// </summary>
        Defeat
    }
}

