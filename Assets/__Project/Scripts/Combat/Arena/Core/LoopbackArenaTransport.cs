using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// In-process transport: host and client are the same machine, messages are delivered
    /// synchronously. Used by the offline arena (vs seeded AI dummies) and by tests — the
    /// networked NGO transport implements the same seam, so the round flow above it is identical.
    /// </summary>
    public class LoopbackArenaTransport : IArenaTransport
    {
        /// <summary>The one in-process connection id (the NGO host convention).</summary>
        private const ulong LocalClientId = 0;

        public event Action<ArenaCommitEnvelope> CommitReceived;
        public event Action<ArenaMatchSetup> MatchSetupReceived;
        public event Action<ArenaRoundBundle> BundleReceived;
        public event Action<ulong, IReadOnlyList<string>> TastedCatalogReceived;
        public event Action<ArenaDraftStart> DraftStartReceived;
        public event Action<ArenaDraftPick> DraftPickRequested;
        public event Action<ArenaDraftPickApplied> DraftPickApplied;
        public event Action<int> PlayerDeparted;
        public event Action<ArenaRejoinPackage> RejoinPackageReceived;
        public event Action<ArenaResyncCommand> ResyncReceived;
        public event Action<ArenaResyncAck> ResyncAckReceived;
        public event Action<ArenaAddressBook> AddressBookReceived;
        public event Action<ulong, ArenaEndpoint> EndpointReported;

        /// <summary>In-process departures only happen when a test (or tool) injects one.</summary>
        public void SimulateDeparture(int playerId)
        {
            PlayerDeparted?.Invoke(playerId);
        }

        public void SubmitCommit(ArenaCommitEnvelope envelope)
        {
            CommitReceived?.Invoke(envelope);
        }

        public void BroadcastMatchSetup(ArenaMatchSetup setup)
        {
            MatchSetupReceived?.Invoke(setup);
        }

        public void BroadcastBundle(ArenaRoundBundle bundle)
        {
            BundleReceived?.Invoke(bundle);
        }

        public void SubmitTastedCatalog(IReadOnlyList<string> partIds)
        {
            TastedCatalogReceived?.Invoke(LocalClientId, partIds);
        }

        public void SubmitDraftPick(ArenaDraftPick pick)
        {
            DraftPickRequested?.Invoke(pick);
        }

        public void BroadcastDraftStart(ArenaDraftStart start)
        {
            DraftStartReceived?.Invoke(start);
        }

        public void BroadcastDraftPick(ArenaDraftPickApplied applied)
        {
            DraftPickApplied?.Invoke(applied);
        }

        // ---- X1 reconnect / resync / migration: the loopback bus is a dumb broadcaster — the
        // targeted messages fire for every subscriber, which filters by TargetPlayerId exactly
        // like a real client ignoring another seat's point-to-point payload would never see it.

        public void SendRejoinPackage(ulong clientId, ArenaRejoinPackage package)
        {
            RejoinPackageReceived?.Invoke(package);
        }

        public void SendResync(ulong clientId, ArenaResyncCommand command)
        {
            ResyncReceived?.Invoke(command);
        }

        public void SubmitResyncAck(ArenaResyncAck ack)
        {
            ResyncAckReceived?.Invoke(ack);
        }

        public void SubmitEndpoint(ArenaEndpoint endpoint)
        {
            EndpointReported?.Invoke(LocalClientId, endpoint);
        }

        public void BroadcastAddressBook(ArenaAddressBook book)
        {
            AddressBookReceived?.Invoke(book);
        }

        public void RemapClient(int playerId, ulong newClientId)
        {
            // In-process play has one connection; there is nothing to remap.
        }
    }
}
