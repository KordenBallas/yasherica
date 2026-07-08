using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Core.Logging;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Combat.Arena.Networking
{
    /// <summary>
    /// Thin session wrapper over the scene's <see cref="NetworkManager"/>: host / join by direct
    /// address (brief R5 — no lobby, no matchmaking), connection approval capping players and
    /// rejecting fresh joins once the match started — a mid-match connection is approved ONLY
    /// when its payload claims a gracing seat through the rejoin gate (X1). Infrastructure only —
    /// the round flow never touches NGO directly.
    /// </summary>
    public class ArenaSessionService : IArenaSessionControl, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly ArenaMatchConfig _config;
        private readonly IGameLogger _logger;

        private IArenaRejoinGate _rejoinGate;
        private bool _callbacksHooked;

        /// <summary>Set when the host starts the match; late joins are rejected from then on.</summary>
        public bool MatchStarted { get; set; }

        public bool IsHost => _networkManager.IsHost;
        public bool IsListening => _networkManager.IsListening;
        public ulong LocalClientId => _networkManager.LocalClientId;
        public IReadOnlyList<ulong> ConnectedClientIds => _networkManager.ConnectedClientsIds.ToList();

        /// <summary>Raised after StartHost/StartClient succeeded — transports register handlers here.</summary>
        public event Action SessionStarted;

        public event Action<ulong> ClientConnected;
        public event Action<ulong> ClientDisconnected;

        public ArenaSessionService(NetworkManager networkManager, ArenaMatchConfig config, IGameLogger logger)
        {
            _networkManager = networkManager;
            _config = config;
            _logger = logger;
        }

        public bool StartHost()
        {
            var transport = _networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData("0.0.0.0", (ushort)_config.Port, "0.0.0.0");

            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.ConnectionApprovalCallback = ApproveConnection;
            HookCallbacks();

            if (!_networkManager.StartHost())
            {
                _logger.Error(LogCategory.Combat, "[ArenaSessionService] StartHost failed");
                return false;
            }

            _logger.Info(LogCategory.Combat, $"[ArenaSessionService] Hosting on port {_config.Port}");
            SessionStarted?.Invoke();
            return true;
        }

        public bool StartClient(string address, byte[] connectPayload = null)
        {
            var (ip, port) = ParseAddress(address);
            var transport = _networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(ip, port);

            // The approval payload: empty = fresh join, a rejoin claim otherwise (X1).
            _networkManager.NetworkConfig.ConnectionData = connectPayload ?? Array.Empty<byte>();

            HookCallbacks();

            if (!_networkManager.StartClient())
            {
                _logger.Error(LogCategory.Combat, $"[ArenaSessionService] StartClient failed for {ip}:{port}");
                return false;
            }

            _logger.Info(LogCategory.Combat, $"[ArenaSessionService] Joining {ip}:{port}");
            SessionStarted?.Invoke();
            return true;
        }

        public void Shutdown()
        {
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
        }

        /// <summary>
        /// Installs the host-side rejoin validator. A setter, not a constructor dependency —
        /// the gate's implementer (the reconnect host) itself depends on this service.
        /// </summary>
        public void SetRejoinGate(IArenaRejoinGate gate)
        {
            _rejoinGate = gate;
        }

        public void Dispose()
        {
            if (_callbacksHooked && _networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= HandleClientConnected;
                _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                _callbacksHooked = false;
            }
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked)
                return;

            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            _callbacksHooked = true;
        }

        private void HandleClientConnected(ulong clientId)
        {
            _logger.Info(LogCategory.Combat, $"[ArenaSessionService] Client {clientId} connected");
            ClientConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _logger.Info(LogCategory.Combat, $"[ArenaSessionService] Client {clientId} disconnected");
            ClientDisconnected?.Invoke(clientId);
        }

        private void ApproveConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            // Heroes are spawned deterministically by the match flow — never as NGO player objects.
            response.CreatePlayerObject = false;

            if (MatchStarted)
            {
                // The one mid-match door: a payload claiming a gracing seat with the right
                // derived token (X1 rejoin). Anything else stays rejected as before.
                if (_rejoinGate != null
                    && ArenaConnectPayload.TryDecodeRejoin(
                        request.Payload, out var playerId, out var token)
                    && _rejoinGate.TryApproveRejoin(playerId, token, request.ClientNetworkId))
                {
                    _logger.Info(LogCategory.Combat,
                        $"[ArenaSessionService] Rejoin approved: player {playerId} on client {request.ClientNetworkId}");
                    response.Approved = true;
                    return;
                }

                response.Approved = false;
                response.Reason = "Match already started";
                return;
            }

            if (_networkManager.ConnectedClientsIds.Count >= _config.MaxPlayers)
            {
                response.Approved = false;
                response.Reason = "Match is full";
                return;
            }

            response.Approved = true;
        }

        private (string ip, ushort port) ParseAddress(string address)
        {
            var trimmed = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            var parts = trimmed.Split(':');
            var ip = string.IsNullOrEmpty(parts[0]) ? "127.0.0.1" : parts[0];
            var port = (ushort)_config.Port;
            if (parts.Length > 1 && ushort.TryParse(parts[1], out var parsedPort))
            {
                port = parsedPort;
            }

            return (ip, port);
        }
    }
}
