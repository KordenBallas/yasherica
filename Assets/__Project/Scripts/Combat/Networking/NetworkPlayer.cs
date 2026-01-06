using Combat.Core;

namespace Combat.Networking
{
    /// <summary>
    /// Network player implementation.
    /// Represents a remote player connected via network.
    /// </summary>
    public class NetworkPlayer : IPlayer
    {
        public int Id { get; }
        public PlayerType Type => PlayerType.Network;
        public string Name { get; }
        
        /// <summary>
        /// Client ID associated with this network player.
        /// </summary>
        public ulong ClientId { get; }
        
        public NetworkPlayer(int id, string name, ulong clientId)
        {
            Id = id;
            Name = name;
            ClientId = clientId;
        }
    }
}

