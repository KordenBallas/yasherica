namespace Combat.Core
{
    /// <summary>
    /// Registry for managing player instances in the game.
    /// Provides access to the local human player for combat initialization.
    /// </summary>
    public interface IPlayerRegistry
    {
        /// <summary>
        /// Gets the local human player.
        /// </summary>
        IPlayer GetLocalPlayer();
        
        /// <summary>
        /// Registers the local human player.
        /// </summary>
        void RegisterLocalPlayer(IPlayer player);
        
        /// <summary>
        /// Unregisters the local human player.
        /// </summary>
        void UnregisterLocalPlayer();
    }
}

