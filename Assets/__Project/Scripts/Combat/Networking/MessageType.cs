namespace Combat.Networking
{
    /// <summary>
    /// Types of network messages.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Player action message.
        /// </summary>
        Action,
        
        /// <summary>
        /// Full game state synchronization.
        /// </summary>
        StateSync,
        
        /// <summary>
        /// Ping message for connection check.
        /// </summary>
        Ping,
        
        /// <summary>
        /// Connection established.
        /// </summary>
        Connected,
        
        /// <summary>
        /// Player disconnected.
        /// </summary>
        Disconnected
    }
}

