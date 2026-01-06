namespace Combat.Core
{
    /// <summary>
    /// Represents a player in the combat system.
    /// Can be Human, AI, or Network player.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Unique identifier for this player.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The type of player (Human, AI, Network).
        /// </summary>
        PlayerType Type { get; }
        
        /// <summary>
        /// Display name of the player.
        /// </summary>
        string Name { get; }
    }
}

