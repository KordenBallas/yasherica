using System;
using System.Collections.Generic;
using Combat.Arena.Core;
using Combat.Core;
using Core.Logging;
using Unity.Collections;
using Unity.Netcode;

namespace Combat.Arena.Networking
{
    /// <summary>
    /// The networked <see cref="IArenaTransport"/>: NGO custom named messages over the direct
    /// connection — no NetworkObjects, no scene sync, no per-unit replication; the lockstep flow
    /// above the seam is byte-identical to the loopback one. The local machine's own messages are
    /// raised directly (host commit → its own collector; broadcasts → its own round loop), remote
    /// ones travel as reliable-fragmented-sequenced named messages (fragmented — the draft board
    /// and a full round bundle exceed the unfragmented MTU; one pipeline = mutual ordering).
    /// </summary>
    public class NgoArenaTransport : IArenaTransport, IDisposable
    {
        private const string CommitMessage = "yash.arena.commit";
        private const string SetupMessage = "yash.arena.setup";
        private const string BundleMessage = "yash.arena.bundle";
        private const string TastedCatalogMessage = "yash.arena.tasted";
        private const string DraftStartMessage = "yash.arena.draftstart";
        private const string DraftPickMessage = "yash.arena.draftpick";
        private const string DraftAppliedMessage = "yash.arena.draftapplied";
        private const int InitialBufferBytes = 1024;
        private const int MaxBufferBytes = 64 * 1024;

        private readonly NetworkManager _networkManager;
        private readonly ArenaSessionService _session;
        private readonly ArenaPlayerDirectory _playerDirectory;
        private readonly IGameLogger _logger;

        private readonly Dictionary<ulong, int> _playerIdByClientId = new Dictionary<ulong, int>();
        private bool _handlersRegistered;

        public event Action<ArenaCommitEnvelope> CommitReceived;
        public event Action<ArenaMatchSetup> MatchSetupReceived;
        public event Action<ArenaRoundBundle> BundleReceived;
        public event Action<ulong, IReadOnlyList<string>> TastedCatalogReceived;
        public event Action<ArenaDraftStart> DraftStartReceived;
        public event Action<ArenaDraftPick> DraftPickRequested;
        public event Action<ArenaDraftPickApplied> DraftPickApplied;
        public event Action<int> PlayerDeparted;

        public NgoArenaTransport(
            NetworkManager networkManager,
            ArenaSessionService session,
            ArenaPlayerDirectory playerDirectory,
            IGameLogger logger)
        {
            _networkManager = networkManager;
            _session = session;
            _playerDirectory = playerDirectory;
            _logger = logger;

            _session.SessionStarted += RegisterHandlers;
            _session.ClientDisconnected += HandleClientDisconnected;
        }

        public void Dispose()
        {
            _session.SessionStarted -= RegisterHandlers;
            _session.ClientDisconnected -= HandleClientDisconnected;
            UnregisterHandlers();
        }

        public void SubmitCommit(ArenaCommitEnvelope envelope)
        {
            if (_session.IsHost)
            {
                // The host is just another player: its lock goes straight to its own collector.
                CommitReceived?.Invoke(envelope);
                return;
            }

            Send(CommitMessage, NetworkManager.ServerClientId, ArenaWireCodec.ToWire(envelope));
        }

        public void BroadcastMatchSetup(ArenaMatchSetup setup)
        {
            RememberRoster(setup);

            var wire = ArenaWireCodec.ToWire(setup);
            foreach (var clientId in RemoteClientIds())
            {
                Send(SetupMessage, clientId, wire);
            }

            MatchSetupReceived?.Invoke(setup);
        }

        public void BroadcastBundle(ArenaRoundBundle bundle)
        {
            var wire = ArenaWireCodec.ToWire(bundle);
            foreach (var clientId in RemoteClientIds())
            {
                Send(BundleMessage, clientId, wire);
            }

            BundleReceived?.Invoke(bundle);
        }

        public void SubmitTastedCatalog(IReadOnlyList<string> partIds)
        {
            if (_session.IsHost)
            {
                // The host is just another participant: its catalog lands straight in its registry.
                TastedCatalogReceived?.Invoke(_session.LocalClientId, partIds);
                return;
            }

            Send(TastedCatalogMessage, NetworkManager.ServerClientId, ArenaWireCodec.ToWire(partIds));
        }

        public void SubmitDraftPick(ArenaDraftPick pick)
        {
            if (_session.IsHost)
            {
                DraftPickRequested?.Invoke(pick);
                return;
            }

            Send(DraftPickMessage, NetworkManager.ServerClientId, ArenaWireCodec.ToWire(pick));
        }

        public void BroadcastDraftStart(ArenaDraftStart start)
        {
            var wire = ArenaWireCodec.ToWire(start);
            foreach (var clientId in RemoteClientIds())
            {
                Send(DraftStartMessage, clientId, wire);
            }

            DraftStartReceived?.Invoke(start);
        }

        public void BroadcastDraftPick(ArenaDraftPickApplied applied)
        {
            var wire = ArenaWireCodec.ToWire(applied);
            foreach (var clientId in RemoteClientIds())
            {
                Send(DraftAppliedMessage, clientId, wire);
            }

            DraftPickApplied?.Invoke(applied);
        }

        private void RegisterHandlers()
        {
            if (_handlersRegistered || _networkManager.CustomMessagingManager == null)
                return;

            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(CommitMessage, HandleCommitMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SetupMessage, HandleSetupMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(BundleMessage, HandleBundleMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(TastedCatalogMessage, HandleTastedCatalogMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DraftStartMessage, HandleDraftStartMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DraftPickMessage, HandleDraftPickMessage);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DraftAppliedMessage, HandleDraftAppliedMessage);
            _handlersRegistered = true;
        }

        private void UnregisterHandlers()
        {
            if (!_handlersRegistered || _networkManager == null || _networkManager.CustomMessagingManager == null)
                return;

            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(CommitMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SetupMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(BundleMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(TastedCatalogMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DraftStartMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DraftPickMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DraftAppliedMessage);
            _handlersRegistered = false;
        }

        private void HandleCommitMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out CommitEnvelopeData data);
            CommitReceived?.Invoke(ArenaWireCodec.FromWire(data, ResolvePlayer));
        }

        private void HandleSetupMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out MatchSetupData data);
            var setup = ArenaWireCodec.FromWire(data);
            RememberRoster(setup);
            MatchSetupReceived?.Invoke(setup);
        }

        private void HandleBundleMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out RoundBundleData data);
            BundleReceived?.Invoke(ArenaWireCodec.FromWire(data, ResolvePlayer));
        }

        private void HandleTastedCatalogMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out TastedCatalogData data);
            TastedCatalogReceived?.Invoke(senderClientId, ArenaWireCodec.FromWire(data));
        }

        private void HandleDraftStartMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out DraftStartData data);
            DraftStartReceived?.Invoke(ArenaWireCodec.FromWire(data));
        }

        private void HandleDraftPickMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out DraftPickData data);
            DraftPickRequested?.Invoke(ArenaWireCodec.FromWire(data));
        }

        private void HandleDraftAppliedMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadNetworkSerializable(out DraftPickAppliedData data);
            DraftPickApplied?.Invoke(ArenaWireCodec.FromWire(data));
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            // Only the host translates departures into the round flow (they ride the next bundle).
            if (!_session.IsHost)
                return;

            if (_playerIdByClientId.TryGetValue(clientId, out var playerId))
            {
                _logger.Info(LogCategory.Combat,
                    $"[NgoArenaTransport] Player {playerId} (client {clientId}) departed");
                PlayerDeparted?.Invoke(playerId);
            }
        }

        private void Send<TMessage>(string messageName, ulong clientId, TMessage message)
            where TMessage : struct, INetworkSerializable
        {
            using var writer = new FastBufferWriter(InitialBufferBytes, Allocator.Temp, MaxBufferBytes);
            writer.WriteNetworkSerializable(in message);
            // ReliableFragmentedSequenced, not ReliableSequenced: the draft board (and a full
            // 4-player round bundle) exceeds the unfragmented MTU (~1264 bytes), and one shared
            // delivery pipeline keeps every message mutually ordered — a fragmented draftstart
            // can never be overtaken by the small draftapplied that follows it.
            _networkManager.CustomMessagingManager.SendNamedMessage(
                messageName, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private IEnumerable<ulong> RemoteClientIds()
        {
            foreach (var clientId in _networkManager.ConnectedClientsIds)
            {
                if (clientId != _networkManager.LocalClientId)
                {
                    yield return clientId;
                }
            }
        }

        private void RememberRoster(ArenaMatchSetup setup)
        {
            _playerIdByClientId.Clear();
            foreach (var slot in setup.Roster)
            {
                _playerIdByClientId[slot.ClientId] = slot.PlayerId;
            }
        }

        private IPlayer ResolvePlayer(int playerId)
        {
            var player = _playerDirectory.Resolve(playerId);
            if (player == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[NgoArenaTransport] No seated player for id {playerId} — was the match set up?");
            }

            return player;
        }
    }
}
