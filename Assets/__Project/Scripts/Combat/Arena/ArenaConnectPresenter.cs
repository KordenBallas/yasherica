using System;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Combat.Arena.Networking;
using Combat.Arena.View;
using Core.Logging;
using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// Pre-match flow (brief R5): Host opens a session and waits for joiners (Start unlocks at
    /// two players); Join connects to an address and waits for the host. The panel hides itself
    /// the moment the match setup arrives — the entrypoint takes over and builds the world.
    /// In offline mode the panel is hidden immediately and the flow never engages.
    /// </summary>
    public class ArenaConnectPresenter : IInitializable, IDisposable
    {
        private const int MinPlayersToStart = 2;

        private readonly IArenaConnectView _view;
        private readonly ArenaSessionService _session;
        private readonly ArenaMatchLauncher _launcher;
        private readonly IArenaTransport _transport;
        private readonly ArenaMatchConfig _config;
        private readonly ArenaMatchContext _matchContext;
        private readonly IGameLogger _logger;

        private bool _hosting;

        public ArenaConnectPresenter(
            IArenaConnectView view,
            ArenaSessionService session,
            ArenaMatchLauncher launcher,
            IArenaTransport transport,
            ArenaMatchConfig config,
            ArenaMatchContext matchContext,
            IGameLogger logger)
        {
            _view = view;
            _session = session;
            _launcher = launcher;
            _transport = transport;
            _config = config;
            _matchContext = matchContext;
            _logger = logger;
        }

        public void Initialize()
        {
            if (_config.OfflineMode)
            {
                _view.Hide();
                return;
            }

            _view.SetStartButtonVisible(false);
            _view.SetStatus("Host a match, or join one by address");

            _view.HostClicked += HandleHostClicked;
            _view.JoinClicked += HandleJoinClicked;
            _view.StartClicked += HandleStartClicked;
            _session.ClientConnected += HandleClientCountChanged;
            _session.ClientDisconnected += HandleClientCountChanged;
            _transport.MatchSetupReceived += HandleMatchSetupReceived;
        }

        public void Dispose()
        {
            _view.HostClicked -= HandleHostClicked;
            _view.JoinClicked -= HandleJoinClicked;
            _view.StartClicked -= HandleStartClicked;
            _session.ClientConnected -= HandleClientCountChanged;
            _session.ClientDisconnected -= HandleClientCountChanged;
            _transport.MatchSetupReceived -= HandleMatchSetupReceived;
        }

        private void HandleHostClicked()
        {
            if (!_session.StartHost())
            {
                _view.SetStatus("Failed to host — see the log");
                return;
            }

            _hosting = true;
            _view.SetConnectControlsInteractable(false);
            _view.SetStartButtonVisible(true);
            RefreshHostStatus();
        }

        private void HandleJoinClicked(string address)
        {
            if (!_session.StartClient(address))
            {
                _view.SetStatus("Failed to connect — see the log");
                return;
            }

            // The dialed address is the X1 reconnect retry target.
            _matchContext.SetHostAddress(address);
            _view.SetConnectControlsInteractable(false);
            _view.SetStatus("Connecting… the host starts the match");
        }

        private void HandleStartClicked()
        {
            _launcher.StartMatch();
        }

        private void HandleClientCountChanged(ulong clientId)
        {
            if (_hosting)
            {
                RefreshHostStatus();
            }
        }

        private void RefreshHostStatus()
        {
            int count = _session.ConnectedClientIds.Count;
            _view.SetStatus($"Hosting — {count}/{_config.MaxPlayers} players connected");
            _view.SetStartButtonInteractable(count >= MinPlayersToStart);
        }

        private void HandleMatchSetupReceived(ArenaMatchSetup setup)
        {
            _logger.Info(LogCategory.Combat, "[ArenaConnectPresenter] Match setup received — closing the panel");
            _view.Hide();
        }
    }
}
