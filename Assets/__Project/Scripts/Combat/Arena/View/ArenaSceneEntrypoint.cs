using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Combat.Arena.Networking;
using Combat.Config;
using Combat.Core;
using Combat.Data.Providers;
using Combat.Execution;
using Combat.Input;
using Combat.Player;
using Combat.View;
using Core.Logging;
using Loot.Core;
using UnityEngine;
using Zenject;

namespace Combat.Arena.View
{
    /// <summary>
    /// Thin bootstrap for the Arena scene. Networked path (default): the connect panel drives
    /// host/join; when the host's match setup arrives, every client seats the roster (its own
    /// seat = the human, the rest = network players) and builds the identical world from the
    /// match seed. Offline path (config flag — dev fallback): seats the local player + seeded AI
    /// dummies immediately. Both paths share the same match build: platform → controller →
    /// telegraph presentation → deterministic spawns → round loop.
    /// </summary>
    public class ArenaSceneEntrypoint : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject] private ArenaMatchConfig _config;
        [Inject] private ArenaPlatformBuilder _platformBuilder;
        [Inject] private ArenaSpawnPlanner _spawnPlanner;
        [Inject] private ArenaHeroSpawner _heroSpawner;
        [Inject] private ArenaCombatController _controller;
        [Inject] private EnemyRoundController _resolvePacer;
        [Inject] private ArenaAICommitSource _aiCommitSource;
        [Inject] private ArenaPlayerDirectory _playerDirectory;
        [Inject] private IArenaTransport _transport;
        [Inject] private ArenaSessionService _session;
        [Inject] private IPlayerRegistry _playerRegistry;
        [Inject] private IInputController _inputController;
        [Inject] private ICombatUnitViewRegistry _unitViewRegistry;
        [Inject] private IAbilityDefinitionCatalog _abilityCatalog;
        [Inject] private IAbilityOutcomeCalculator _outcomeCalculator;
        [Inject] private HexDirectionConfig _hexDirectionConfig;
        [Inject] private IGameLogger _logger;

        private UnitOverheadIconsView _planIconsView;
        private UnitPlanIconsPresenter _planIconsPresenter;
        private GhostPlaybackView _ghostView;
        private GhostPlaybackPresenter _ghostPresenter;
        private bool _matchStarted;

        public void Initialize()
        {
            if (_config.OfflineMode)
            {
                StartOfflineMatch();
                return;
            }

            _transport.MatchSetupReceived += HandleMatchSetupReceived;
        }

        public void Dispose()
        {
            _transport.MatchSetupReceived -= HandleMatchSetupReceived;
            _aiCommitSource?.Dispose();
            _resolvePacer?.Dispose();
            _planIconsPresenter?.Dispose();
            _ghostPresenter?.Dispose();
            _playerRegistry?.UnregisterLocalPlayer();
            _session?.Shutdown();
        }

        // ---- offline (dev fallback) ----

        private void StartOfflineMatch()
        {
            int matchSeed = _config.OfflineMatchSeed != 0 ? _config.OfflineMatchSeed : Environment.TickCount;
            _logger.Info(LogCategory.Combat, $"[ArenaSceneEntrypoint] Offline arena match, seed {matchSeed}");

            var players = SeatOfflinePlayers(matchSeed);
            StartMatch(matchSeed, players);
        }

        private List<IPlayer> SeatOfflinePlayers(int matchSeed)
        {
            var human = new HumanPlayer(1, "Player 1");
            var players = new List<IPlayer> { human };

            int dummyCount = Mathf.Clamp(_config.OfflineDummyCount, 1, _config.MaxPlayers - 1);
            for (int i = 0; i < dummyCount; i++)
            {
                int playerId = players.Count + 1;
                // Seat index == unit id in the MVP (one hero per player), so the AI seed context
                // matches the unit the way PvE's per-enemy seeds do.
                var aiSeed = LootSeed.Derive(matchSeed, $"arena-ai:{playerId}");
                players.Add(new AIPlayer(playerId, $"Dummy {playerId}", new TacticalAI(aiSeed, _logger)));
            }

            return players;
        }

        // ---- networked ----

        private void HandleMatchSetupReceived(ArenaMatchSetup setup)
        {
            if (_matchStarted)
                return;

            _logger.Info(LogCategory.Combat,
                $"[ArenaSceneEntrypoint] Match setup: {setup.Roster.Count} players, seed {setup.MatchSeed}, " +
                $"{(_session.IsHost ? "hosting" : "joined")}");

            var players = SeatNetworkedPlayers(setup);
            _controller.SetHostRole(_session.IsHost);
            StartMatch(setup.MatchSeed, players);
        }

        private List<IPlayer> SeatNetworkedPlayers(ArenaMatchSetup setup)
        {
            var players = new List<IPlayer>();
            foreach (var slot in setup.Roster.OrderBy(s => s.PlayerId))
            {
                if (slot.ClientId == _session.LocalClientId)
                {
                    players.Add(new HumanPlayer(slot.PlayerId, $"Player {slot.PlayerId}"));
                }
                else
                {
                    players.Add(new Combat.Networking.NetworkPlayer(
                        slot.PlayerId, $"Player {slot.PlayerId}", slot.ClientId));
                }
            }

            return players;
        }

        // ---- shared match build ----

        private void StartMatch(int matchSeed, List<IPlayer> players)
        {
            _matchStarted = true;

            var localPlayer = players.First(p => p.Type == PlayerType.Human);
            _playerRegistry.RegisterLocalPlayer(localPlayer);
            _playerDirectory.Set(players);

            var platform = _platformBuilder.Build(matchSeed);
            _controller.InitializeBattlefield(platform.Surface, platform.Position);

            var initialState = new CombatState(
                new List<IUnit>(), players, players[0], 1, CombatPhase.Combat, null);
            _controller.Initialize(initialState, players);

            _resolvePacer.Initialize(_controller, this);
            if (players.Any(p => p is AIPlayer))
            {
                _aiCommitSource.Initialize(_controller);
            }

            CreateTelegraphPresentation();

            var spawnCells = _spawnPlanner.Plan(
                platform.Surface.Cells, platform.Surface.CenterCell, players.Count);
            var slots = players
                .Select((p, i) => new ArenaSpawnSlot(p, i + 1, spawnCells[i], p.Type == PlayerType.Human))
                .ToList();
            _heroSpawner.SpawnAll(slots, _controller.Battlefield, _controller);

            // Every seat is on the board — the last-standing check may go live.
            _controller.ArmWinCondition();
            _controller.BeginRounds();
            _inputController.Enable();
        }

        private void CreateTelegraphPresentation()
        {
            // Same construction CombatActiveState does for PvE — the presenters read the
            // ICombatController seam, so the arena reveal/ghosts come for free.
            var planIconsGo = new GameObject("UnitOverheadIconsView");
            _planIconsView = planIconsGo.AddComponent<UnitOverheadIconsView>();
            _planIconsView.Initialize(_unitViewRegistry, _abilityCatalog);
            _planIconsPresenter = new UnitPlanIconsPresenter(_controller, _planIconsView);

            var ghostGo = new GameObject("GhostPlaybackView");
            _ghostView = ghostGo.AddComponent<GhostPlaybackView>();
            _ghostView.Initialize(_unitViewRegistry);
            _ghostPresenter = new GhostPlaybackPresenter(
                _controller,
                _outcomeCalculator,
                _hexDirectionConfig,
                coords => _controller.Battlefield.HexToWorld(coords),
                _ghostView,
                _logger);
            var hoverController = ghostGo.AddComponent<AbilityIconHoverController>();
            hoverController.Initialize(_ghostPresenter);
        }

    }
}
