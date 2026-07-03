using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Data;
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
    /// Thin bootstrap for the Arena scene (Phase 2 — offline mode): rolls the match seed, builds
    /// the platform, seats the local player + seeded AI dummies, spawns the heroes, wires the
    /// PvE telegraph presentation (plan icons + ghost) and the resolve pacing, then starts the
    /// symmetric round loop. The networked host/join path replaces the seating step in Phase 3.
    /// </summary>
    public class ArenaSceneEntrypoint : MonoBehaviour, IInitializable, IDisposable
    {
        [Tooltip("Optional status line (round / winner); skipped when unwired.")]
        [SerializeField] private TMPro.TMP_Text _statusText;

        [Inject] private ArenaMatchConfig _config;
        [Inject] private ArenaPlatformBuilder _platformBuilder;
        [Inject] private ArenaSpawnPlanner _spawnPlanner;
        [Inject] private ArenaHeroSpawner _heroSpawner;
        [Inject] private ArenaCombatController _controller;
        [Inject] private EnemyRoundController _resolvePacer;
        [Inject] private ArenaAICommitSource _aiCommitSource;
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

        public void Initialize()
        {
            int matchSeed = _config.OfflineMatchSeed != 0 ? _config.OfflineMatchSeed : Environment.TickCount;
            _logger.Info(LogCategory.Combat, $"[ArenaSceneEntrypoint] Offline arena match, seed {matchSeed}");

            var platform = _platformBuilder.Build(matchSeed);
            _controller.InitializeBattlefield(platform.Surface, platform.Position);

            var players = SeatPlayers(matchSeed);
            var initialState = new CombatState(
                new List<IUnit>(), players, players[0], 1, CombatPhase.Combat, null);
            _controller.Initialize(initialState, players);
            _controller.OnGameEnded += HandleGameEnded;
            _controller.OnTurnStarted += HandleTurnStarted;

            _resolvePacer.Initialize(_controller, this);
            _aiCommitSource.Initialize(_controller);
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

        public void Dispose()
        {
            if (_controller != null)
            {
                _controller.OnGameEnded -= HandleGameEnded;
                _controller.OnTurnStarted -= HandleTurnStarted;
            }

            _aiCommitSource?.Dispose();
            _resolvePacer?.Dispose();
            _planIconsPresenter?.Dispose();
            _ghostPresenter?.Dispose();
            _playerRegistry?.UnregisterLocalPlayer();
        }

        private List<IPlayer> SeatPlayers(int matchSeed)
        {
            var human = new HumanPlayer(1, "Player 1");
            _playerRegistry.RegisterLocalPlayer(human);

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

        private void HandleTurnStarted(IPlayer player)
        {
            SetStatus($"Round {_controller.CombatState.TurnNumber} — plan your actions");
        }

        private void HandleGameEnded(IPlayer winner, CombatPhase phase)
        {
            var message = winner != null
                ? $"{winner.Name} is the last hero standing!"
                : "Draw — no heroes left standing";
            _logger.Info(LogCategory.Combat, $"[ArenaSceneEntrypoint] {message}");
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }
    }
}
