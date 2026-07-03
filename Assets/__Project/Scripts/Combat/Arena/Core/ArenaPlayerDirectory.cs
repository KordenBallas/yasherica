using System.Collections.Generic;
using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The seated players of the current match, by PlayerId — filled once at seating and read by
    /// the network transport to give decoded intents their owning <see cref="IPlayer"/> instance
    /// (actions carry their player; every client resolves against its own seat objects).
    /// </summary>
    public class ArenaPlayerDirectory
    {
        private readonly Dictionary<int, IPlayer> _players = new Dictionary<int, IPlayer>();

        public void Set(IReadOnlyList<IPlayer> players)
        {
            _players.Clear();
            foreach (var player in players)
            {
                _players[player.Id] = player;
            }
        }

        public IPlayer Resolve(int playerId)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }

        public IReadOnlyList<IPlayer> All => _players.Values.OrderBy(p => p.Id).ToList();
    }
}
