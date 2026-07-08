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

        /// <summary>Client side: hand the local tasted-forms catalog to the host (pre-draft).</summary>
        void SubmitTastedCatalog(IReadOnlyList<string> partIds);

        /// <summary>Client side: request one draft pick from the host authority.</summary>
        void SubmitDraftPick(ArenaDraftPick pick);

        /// <summary>Host side: open the draft with the composed board on every client (host included).</summary>
        void BroadcastDraftStart(ArenaDraftStart start);

        /// <summary>Host side: broadcast one canonically applied pick to every client (host included).</summary>
        void BroadcastDraftPick(ArenaDraftPickApplied applied);

        // ---- X1 reconnect / resync / migration ----

        /// <summary>Host side: hand a rejoiner the full match stand-up (point-to-point).</summary>
        void SendRejoinPackage(ulong clientId, ArenaRejoinPackage package);

        /// <summary>Host side: heal a diverged but connected client (point-to-point).</summary>
        void SendResync(ulong clientId, ArenaResyncCommand command);

        /// <summary>Client side: acknowledge a completed state transfer to the host.</summary>
        void SubmitResyncAck(ArenaResyncAck ack);

        /// <summary>Client side: self-report the reachable address for the migration book.</summary>
        void SubmitEndpoint(ArenaEndpoint endpoint);

        /// <summary>Host side: broadcast the migration address book to every client.</summary>
        void BroadcastAddressBook(ArenaAddressBook book);

        /// <summary>
        /// Host side: a rejoin-approved seat now lives on a new connection — future departures of
        /// that connection must map back to the seat. No-op for the in-process loopback.
        /// </summary>
        void RemapClient(int playerId, ulong newClientId);

        /// <summary>Raised on the host for every arriving commit (its own included).</summary>
        event Action<ArenaCommitEnvelope> CommitReceived;

        /// <summary>Raised on every client (host included) when the match setup arrives.</summary>
        event Action<ArenaMatchSetup> MatchSetupReceived;

        /// <summary>Raised on every client (host included) when a round bundle arrives.</summary>
        event Action<ArenaRoundBundle> BundleReceived;

        /// <summary>Raised on the host for every arriving tasted catalog (its own included).</summary>
        event Action<ulong, IReadOnlyList<string>> TastedCatalogReceived;

        /// <summary>Raised on every client (host included) when the draft opens.</summary>
        event Action<ArenaDraftStart> DraftStartReceived;

        /// <summary>Raised on the host for every arriving pick request (its own included).</summary>
        event Action<ArenaDraftPick> DraftPickRequested;

        /// <summary>Raised on every client (host included) when the host applies a pick.</summary>
        event Action<ArenaDraftPickApplied> DraftPickApplied;

        /// <summary>Raised on the host when a player's connection is gone (never in offline play).</summary>
        event Action<int> PlayerDeparted;

        // ---- X1 reconnect / resync / migration events ----

        /// <summary>Raised on a client when a rejoin package arrives (filter by TargetPlayerId).</summary>
        event Action<ArenaRejoinPackage> RejoinPackageReceived;

        /// <summary>Raised on a client when a resync command arrives (filter by TargetPlayerId).</summary>
        event Action<ArenaResyncCommand> ResyncReceived;

        /// <summary>Raised on the host when a rejoiner acknowledged its state transfer.</summary>
        event Action<ArenaResyncAck> ResyncAckReceived;

        /// <summary>Raised on every client when the migration address book arrives.</summary>
        event Action<ArenaAddressBook> AddressBookReceived;

        /// <summary>Raised on the host for every self-reported endpoint (sender's connection id rides along).</summary>
        event Action<ulong, ArenaEndpoint> EndpointReported;
    }
}
