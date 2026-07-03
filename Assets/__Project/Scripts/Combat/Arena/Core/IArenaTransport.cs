using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// One player's lock message: the commit plus the previous round's state hash (R10 —
    /// piggybacked so the host can detect lockstep divergence one round late; 0 for round 1).
    /// </summary>
    public class ArenaCommitEnvelope
    {
        public int RoundNumber { get; }
        public ArenaCommit Commit { get; }
        public ulong PreviousRoundStateHash { get; }

        public ArenaCommitEnvelope(int roundNumber, ArenaCommit commit, ulong previousRoundStateHash)
        {
            RoundNumber = roundNumber;
            Commit = commit;
            PreviousRoundStateHash = previousRoundStateHash;
        }
    }

    /// <summary>One match seat: which connection plays which player/unit ids.</summary>
    public class ArenaRosterSlot
    {
        public ulong ClientId { get; }
        public int PlayerId { get; }
        public int UnitId { get; }

        public ArenaRosterSlot(ulong clientId, int playerId, int unitId)
        {
            ClientId = clientId;
            PlayerId = playerId;
            UnitId = unitId;
        }
    }

    /// <summary>
    /// The host's match handshake: the seed every client derives the platform/spawns from, and
    /// the seat assignment. Spawn cells are NOT on the wire — they derive deterministically from
    /// the seed + roster size on every client.
    /// </summary>
    public class ArenaMatchSetup
    {
        public int MatchSeed { get; }
        public IReadOnlyList<ArenaRosterSlot> Roster { get; }

        public ArenaMatchSetup(int matchSeed, IReadOnlyList<ArenaRosterSlot> roster)
        {
            MatchSeed = matchSeed;
            Roster = roster ?? new List<ArenaRosterSlot>();
        }
    }

    /// <summary>
    /// The engine-free wire seam of the lockstep flow. Client→host: locked commits. Host→all:
    /// match setup and assembled round bundles. Implementations: in-process loopback (offline
    /// mode + tests) and the NGO relay (networked play).
    /// </summary>
    public interface IArenaTransport
    {
        /// <summary>Client side: send the local player's locked round to the host.</summary>
        void SubmitCommit(ArenaCommitEnvelope envelope);

        /// <summary>Host side: announce the match seed + seats to every client.</summary>
        void BroadcastMatchSetup(ArenaMatchSetup setup);

        /// <summary>Host side: broadcast the assembled round to every client (host included).</summary>
        void BroadcastBundle(ArenaRoundBundle bundle);

        /// <summary>Raised on the host for every arriving commit (its own included).</summary>
        event Action<ArenaCommitEnvelope> CommitReceived;

        /// <summary>Raised on every client (host included) when the match setup arrives.</summary>
        event Action<ArenaMatchSetup> MatchSetupReceived;

        /// <summary>Raised on every client (host included) when a round bundle arrives.</summary>
        event Action<ArenaRoundBundle> BundleReceived;

        /// <summary>Raised on the host when a player's connection is gone (never in offline play).</summary>
        event Action<int> PlayerDeparted;
    }
}
