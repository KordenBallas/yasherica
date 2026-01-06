namespace Combat.Core
{
    /// <summary>
    /// Defines the type of player.
    /// </summary>
    public enum PlayerType
    {
        /// <summary>
        /// Human player with local input.
        /// </summary>
        Human,
        
        /// <summary>
        /// AI-controlled player.
        /// </summary>
        AI,
        
        /// <summary>
        /// Network player (remote human).
        /// </summary>
        Network
    }
}

