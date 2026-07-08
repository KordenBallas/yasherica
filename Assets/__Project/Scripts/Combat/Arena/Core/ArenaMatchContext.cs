using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The live match's identity, held once per scene: the seed + seat roster from the host's
    /// setup and the confirmed draft loadouts. Everything a rejoining peer cannot re-derive
    /// locally rides FROM here (rejoin package assembly, rejoin-token derivation, seeded
    /// resolution strategies). Populated by the scene entrypoint as the match assembles;
    /// empty until then.
    /// </summary>
    public class ArenaMatchContext
    {
        private readonly List<ArenaRosterSlot> _roster = new List<ArenaRosterSlot>();
        private readonly Dictionary<int, IReadOnlyDictionary<string, string>> _loadoutByPlayerId =
            new Dictionary<int, IReadOnlyDictionary<string, string>>();

        public int MatchSeed { get; private set; }
        public IReadOnlyList<ArenaRosterSlot> Roster => _roster;
        public IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> LoadoutByPlayerId =>
            _loadoutByPlayerId;

        /// <summary>The seat this machine plays; 0 until seated.</summary>
        public int LocalPlayerId { get; private set; }

        /// <summary>The address this client dialed ("" on the hosting machine) — the reconnect retry target.</summary>
        public string HostAddress { get; private set; } = string.Empty;

        public bool HasSetup => _roster.Count > 0;

        public void SetLocalPlayerId(int playerId)
        {
            LocalPlayerId = playerId;
        }

        public void SetHostAddress(string address)
        {
            HostAddress = address ?? string.Empty;
        }

        public void SetSetup(int matchSeed, IReadOnlyList<ArenaRosterSlot> roster)
        {
            MatchSeed = matchSeed;
            _roster.Clear();
            if (roster != null)
            {
                _roster.AddRange(roster);
            }
        }

        public void SetLoadouts(
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> loadoutByPlayerId)
        {
            _loadoutByPlayerId.Clear();
            if (loadoutByPlayerId == null)
                return;

            foreach (var pair in loadoutByPlayerId)
            {
                _loadoutByPlayerId[pair.Key] = pair.Value;
            }
        }

        public bool TryGetSlot(int playerId, out ArenaRosterSlot slot)
        {
            foreach (var candidate in _roster)
            {
                if (candidate.PlayerId == playerId)
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = null;
            return false;
        }
    }
}
