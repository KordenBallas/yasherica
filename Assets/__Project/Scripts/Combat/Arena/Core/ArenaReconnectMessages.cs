using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Host → one rejoiner: everything a peer needs to stand a live match up from nothing —
    /// the original setup (seed + seats), the draft loadouts, the authoritative round-start
    /// snapshot, and (when the current round's bundle was assembled before the rejoiner
    /// attached) that bundle for replay. Targeted: receivers ignore a package addressed to
    /// another seat (the loopback bus is a dumb broadcaster; NGO also sends point-to-point).
    /// </summary>
    public class ArenaRejoinPackage
    {
        public int TargetPlayerId { get; }
        public ArenaMatchSetup Setup { get; }
        public IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> LoadoutByPlayerId { get; }
        public ArenaStateSnapshot Snapshot { get; }
        public IReadOnlyList<int> DepartedPlayerIds { get; }

        /// <summary>The current round's bundle when it broadcast before the rejoin completed; null otherwise.</summary>
        public ArenaRoundBundle CurrentRoundBundleOrNull { get; }

        public ArenaRejoinPackage(
            int targetPlayerId,
            ArenaMatchSetup setup,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> loadoutByPlayerId,
            ArenaStateSnapshot snapshot,
            IReadOnlyList<int> departedPlayerIds,
            ArenaRoundBundle currentRoundBundleOrNull)
        {
            TargetPlayerId = targetPlayerId;
            Setup = setup;
            LoadoutByPlayerId = loadoutByPlayerId
                ?? new Dictionary<int, IReadOnlyDictionary<string, string>>();
            Snapshot = snapshot;
            DepartedPlayerIds = departedPlayerIds ?? new List<int>();
            CurrentRoundBundleOrNull = currentRoundBundleOrNull;
        }
    }

    /// <summary>
    /// Host → one still-connected but diverged client (R10 heal): adopt this authoritative
    /// round-start snapshot and re-plan. Targeted like the rejoin package.
    /// </summary>
    public class ArenaResyncCommand
    {
        public int TargetPlayerId { get; }
        public ArenaStateSnapshot Snapshot { get; }

        public ArenaResyncCommand(int targetPlayerId, ArenaStateSnapshot snapshot)
        {
            TargetPlayerId = targetPlayerId;
            Snapshot = snapshot;
        }
    }

    /// <summary>Client → host: the state transfer landed; the seat may go live again.</summary>
    public class ArenaResyncAck
    {
        public int PlayerId { get; }
        public int RoundNumber { get; }

        public ArenaResyncAck(int playerId, int roundNumber)
        {
            PlayerId = playerId;
            RoundNumber = roundNumber;
        }
    }

    /// <summary>One peer's self-reported reachable address ("ip" or "ip:port"; LAN best-effort).</summary>
    public class ArenaEndpoint
    {
        public int PlayerId { get; }
        public string Address { get; }

        public ArenaEndpoint(int playerId, string address)
        {
            PlayerId = playerId;
            Address = address;
        }
    }

    /// <summary>
    /// Host → all: every peer's claimed endpoint, so any survivor of a host drop can locate the
    /// elected replacement (X1 migration). Best-effort by design — NAT defeats self-reported
    /// addresses until X3 brings a relay.
    /// </summary>
    public class ArenaAddressBook
    {
        public IReadOnlyList<ArenaEndpoint> Endpoints { get; }

        public ArenaAddressBook(IReadOnlyList<ArenaEndpoint> endpoints)
        {
            Endpoints = endpoints ?? new List<ArenaEndpoint>();
        }
    }
}
