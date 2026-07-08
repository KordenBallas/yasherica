using System;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Networking;
using Combat.Arena.View;
using Combat.Core;
using Combat.Input;
using Core.Logging;
using Core.SceneFlow;
using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// The in-match HUD flow: round/status line ("plan your actions" → "locked in — waiting"),
    /// defeat → spectate (input off, label on — the match keeps resolving in front of the player),
    /// the winner/draw banner with Leave → main menu, and the R10 desync warning on the host.
    /// A lost connection is no longer terminal (X1): the reconnect machine drives the status line
    /// while it retries/migrates, input freezes for the duration, and only
    /// <see cref="ArenaReconnectClient.ReconnectFailed"/> lands on the old "connection lost" end
    /// state. Seat notices surface other players' drops off the bundle stream.
    /// </summary>
    public class ArenaMatchHudPresenter : IInitializable, IDisposable
    {
        private readonly IArenaMatchHudView _view;
        private readonly ArenaCombatController _controller;
        private readonly ArenaMatchHost _matchHost;
        private readonly ArenaSessionService _session;
        private readonly ArenaReconnectClient _reconnectClient;
        private readonly IArenaTransport _transport;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IInputController _inputController;
        private readonly ISceneLoader _sceneLoader;
        private readonly IGameLogger _logger;

        private bool _spectating;
        private bool _matchOver;
        private bool _reconnecting;

        public ArenaMatchHudPresenter(
            IArenaMatchHudView view,
            ArenaCombatController controller,
            ArenaMatchHost matchHost,
            ArenaSessionService session,
            ArenaReconnectClient reconnectClient,
            IArenaTransport transport,
            IPlayerRegistry playerRegistry,
            IInputController inputController,
            ISceneLoader sceneLoader,
            IGameLogger logger)
        {
            _view = view;
            _controller = controller;
            _matchHost = matchHost;
            _session = session;
            _reconnectClient = reconnectClient;
            _transport = transport;
            _playerRegistry = playerRegistry;
            _inputController = inputController;
            _sceneLoader = sceneLoader;
            _logger = logger;
        }

        public void Initialize()
        {
            _view.SetSpectatingVisible(false);
            _view.SetLeaveVisible(false);
            _view.SetSeatNotice(null);

            _view.LeaveClicked += HandleLeaveClicked;
            _controller.OnStateChanged += HandleStateChanged;
            _controller.OnGameEnded += HandleGameEnded;
            _matchHost.DesyncDetected += HandleDesyncDetected;
            _matchHost.CommitRejected += HandleCommitRejected;
            _reconnectClient.PhaseChanged += HandleReconnectPhaseChanged;
            _reconnectClient.Reconnected += HandleReconnected;
            _reconnectClient.ReconnectFailed += HandleReconnectFailed;
            _transport.BundleReceived += HandleBundleReceived;
            _session.ClientDisconnected += HandleClientDisconnected;
        }

        public void Dispose()
        {
            _view.LeaveClicked -= HandleLeaveClicked;
            _controller.OnStateChanged -= HandleStateChanged;
            _controller.OnGameEnded -= HandleGameEnded;
            _matchHost.DesyncDetected -= HandleDesyncDetected;
            _matchHost.CommitRejected -= HandleCommitRejected;
            _reconnectClient.PhaseChanged -= HandleReconnectPhaseChanged;
            _reconnectClient.Reconnected -= HandleReconnected;
            _reconnectClient.ReconnectFailed -= HandleReconnectFailed;
            _transport.BundleReceived -= HandleBundleReceived;
            _session.ClientDisconnected -= HandleClientDisconnected;
        }

        private void HandleStateChanged(ICombatState state)
        {
            if (_matchOver || _reconnecting || state == null || state.Phase != CombatPhase.Combat)
                return;

            UpdateSpectateState(state);
            UpdateStatusLine(state);
        }

        private void UpdateSpectateState(ICombatState state)
        {
            if (_spectating)
                return;

            var localPlayer = _playerRegistry.GetLocalPlayer();
            if (localPlayer == null)
                return;

            var units = state.GetUnitsByPlayer(localPlayer);
            if (units.Count == 0 || units.Any(u => u.IsAlive))
                return;

            // The local hero fell but the match goes on: the player watches it end (brief R14).
            _spectating = true;
            _inputController.Disable();
            _view.SetSpectatingVisible(true);
            _logger.Info(LogCategory.Combat, "[ArenaMatchHud] Local hero defeated — spectating");
        }

        private void UpdateStatusLine(ICombatState state)
        {
            if (_spectating)
            {
                _view.SetStatus($"Round {state.TurnNumber} — spectating");
                return;
            }

            if (state.RoundPhase != RoundPhase.PlayerAct)
            {
                _view.SetStatus($"Round {state.TurnNumber} — resolving…");
                return;
            }

            var localPlayer = _playerRegistry.GetLocalPlayer();
            bool lockedIn = localPlayer != null
                && state.GetUnitsByPlayer(localPlayer).All(u => !u.IsAlive || u.HasActedThisTurn);

            _view.SetStatus(lockedIn
                ? $"Round {state.TurnNumber} — locked in, waiting for the others…"
                : $"Round {state.TurnNumber} — plan your actions");
        }

        private void HandleGameEnded(IPlayer winner, CombatPhase phase)
        {
            _matchOver = true;
            _inputController.Disable();

            var localPlayer = _playerRegistry.GetLocalPlayer();
            string banner = winner == null
                ? "Draw — no heroes left standing"
                : winner.Id == localPlayer?.Id
                    ? "Victory — you are the last hero standing!"
                    : $"{winner.Name} is the last hero standing";

            _view.SetSpectatingVisible(false);
            _view.SetSeatNotice(null);
            _view.SetStatus(banner);
            _view.SetLeaveVisible(true);
        }

        // ---- X1: the reconnect machine drives the line while the connection is down ----

        private void HandleReconnectPhaseChanged(ArenaReconnectPhase phase, string status)
        {
            if (_matchOver)
                return;

            switch (phase)
            {
                case ArenaReconnectPhase.RetryingHost:
                case ArenaReconnectPhase.ConnectingToCandidate:
                    _reconnecting = true;
                    _inputController.Disable();
                    _view.SetStatus(status);
                    break;

                case ArenaReconnectPhase.InMatch when _reconnecting:
                    // Handled by Reconnected (also covers the promoted-host copy).
                    _view.SetStatus(status);
                    break;
            }
        }

        private void HandleReconnected()
        {
            if (_matchOver)
                return;

            _reconnecting = false;
            if (!_spectating)
            {
                _inputController.Enable();
            }

            _logger.Info(LogCategory.Combat, "[ArenaMatchHud] Back in the match");
        }

        private void HandleReconnectFailed()
        {
            if (_matchOver)
                return;

            _matchOver = true;
            _inputController.Disable();
            _view.SetStatus("Connection to the match was lost");
            _view.SetLeaveVisible(true);
            _logger.Warning(LogCategory.Combat, "[ArenaMatchHud] Reconnect failed — match over");
        }

        private void HandleBundleReceived(ArenaRoundBundle bundle)
        {
            if (_matchOver)
                return;

            if (bundle.AutoPassedPlayerIds.Count > 0)
            {
                var names = string.Join(", ", bundle.AutoPassedPlayerIds.Select(id => $"Player {id}"));
                _view.SetSeatNotice($"{names} disconnected — auto-passing");
            }
            else
            {
                _view.SetSeatNotice(null);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            // Pre-combat drops (the reconnect machine is armed only once combat begins) keep the
            // MVP terminal behavior: a host lost during connect/draft ends the session.
            if (_matchOver || _session.IsHost || clientId != _session.LocalClientId)
                return;
            if (_reconnectClient.Phase != ArenaReconnectPhase.Idle)
                return;

            _matchOver = true;
            _inputController.Disable();
            _view.SetStatus("Connection to the host was lost");
            _view.SetLeaveVisible(true);
            _logger.Warning(LogCategory.Combat, "[ArenaMatchHud] Disconnected from the host — match over");
        }

        private void HandleDesyncDetected(int playerId)
        {
            _view.ShowDesyncWarning();
        }

        private void HandleCommitRejected(int playerId, string reason)
        {
            // Host-side only (the event lives on the assembler): the substituted pass itself
            // rides the bundle for everyone.
            _view.SetSeatNotice($"Player {playerId}'s action was rejected — passed this round");
        }

        private void HandleLeaveClicked()
        {
            _logger.Info(LogCategory.Combat, "[ArenaMatchHud] Leaving the arena");
            _session.Shutdown();
            _sceneLoader.Load(SceneNames.MainMenu);
        }
    }
}
