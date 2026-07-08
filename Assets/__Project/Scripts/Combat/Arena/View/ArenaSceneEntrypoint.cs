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
    /// seat = the human, the rest = network players). Offline path (config flag — dev
    /// fallback): seats the local player + seeded AI dummies immediately. Both paths then run
    /// the PARTS DRAFT (P4-5) — the board opens off the host's broadcast, every seat drafts a
    /// body — and only the confirmed draft result starts the shared match build: platform →
    /// controller → telegraph presentation → deterministic spawns (drafted loadouts) → the
    /// untouched round loop.
    /// </summary>
    public class ArenaSceneEntrypoint : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject] private ArenaMatchConfig _config;
        [Inject] private ArenaPlatformBuilder _platformBuilder;
        [Inject] private ArenaSpawnPlanner _spawnPlanner;
        [Inject] private ArenaHeroSpawner _heroSpawner;
        [Inject] private ArenaCombatController _controller;
        [Inject] private ArenaMatchHost _matchHost;
        [Inject] private EnemyRoundController _resolvePacer;
        [Inject] private ArenaAICommitSource _aiCommitSource;
        [Inject] private ArenaPlayerDirectory _playerDirectory;
        [Inject] private IArenaTransport _transport;
        [Inject] private ArenaSessionService _session;
        [Inject] private IPlayerRegistry _playerRegistry;
        [Inject] private IInputController _inputController;
        [Inject] private ICombatUnitViewRegistry _unitViewRegistry;
        [Inject] private IAbilityDefinitionCatalog _abilityCatalog;
        [Inject] private IStatusEffectDefinitionCatalog _statusCatalog;
        [Inject] private IAbilityOutcomeCalculator _outcomeCalculator;
        [Inject] private Player.AI.AIDecisionMakerFactory _decisionMakerFactory;
        [Inject] private HexDirectionConfig _hexDirectionConfig;
        [Inject] private ArenaDraftFlow _draftFlow;
        [Inject] private ArenaDraftHost _draftHost;
        [Inject] private ArenaTastedCatalogSender _catalogSender;
        [Inject] private ArenaMatchContext _matchContext;
        [Inject] private ArenaReconnectHost _reconnectHost;
        [Inject] private ArenaReconnectClient _reconnectClient;
        [Inject] private ArenaCommitValidator _commitValidator;
        [Inject] private ArenaQueueCreditLedger _queueCredits;
        [Inject] private ArenaBatchEligibility _batchEligibility;
        [Inject] private CombatConfig _combatConfig;
        [Inject] private IGameLogger _logger;

        private UnitOverheadIconsView _planIconsView;
        private UnitPlanIconsPresenter _planIconsPresenter;
        private UnitStatusIconsView _statusIconsView;
        private UnitStatusIconsPresenter _statusIconsPresenter;
        private GhostPlaybackView _ghostView;
        private GhostPlaybackPresenter _ghostPresenter;
        private bool _matchStarted;
        private int _matchSeed;
        private List<IPlayer> _players;

        public void Initialize()
        {
            _draftFlow.ReadyForCombat += HandleDraftReady;

            if (_config.OfflineMode)
            {
                StartOfflineMatch();
                return;
            }

            _transport.MatchSetupReceived += HandleMatchSetupReceived;
        }

        public void Dispose()
        {
            _reconnectClient?.Disarm();
            _reconnectHost?.Deactivate();
            _transport.MatchSetupReceived -= HandleMatchSetupReceived;
            _draftFlow.ReadyForCombat -= HandleDraftReady;
            _aiCommitSource?.Dispose();
            _resolvePacer?.Dispose();
            _planIconsPresenter?.Dispose();
            _statusIconsPresenter?.Dispose();
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
            BeginDraft(matchSeed, players);

            // Offline has no session handshake: this machine is the host — submit the local
            // catalog and open the draft directly (dummies auto-pick on the AI delay).
            _catalogSender.SubmitNow();
            var roster = players
                .Select(p => new ArenaRosterSlot(0, p.Id, p.Id))
                .ToList();
            _matchContext.SetSetup(matchSeed, roster);
            _draftHost.StartDraft(
                matchSeed, roster, players.Where(p => p is AIPlayer).Select(p => p.Id).ToList());
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
                players.Add(new AIPlayer(playerId, $"Dummy {playerId}",
                    _decisionMakerFactory.Create(Player.AI.AIBehaviorProfile.Default, aiSeed)));
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
            _matchContext.SetSetup(setup.MatchSeed, setup.Roster);
            _controller.SetHostRole(_session.IsHost);
            BeginDraft(setup.MatchSeed, players);
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

        // ---- the draft gate ----

        /// <summary>
        /// Seats are known — hand over to the draft. The match build waits for the confirmed
        /// draft result; combat input stays disabled throughout (the draft is pure UI).
        /// </summary>
        private void BeginDraft(int matchSeed, List<IPlayer> players)
        {
            _matchStarted = true;
            _matchSeed = matchSeed;
            _players = players;

            var localPlayer = players.First(p => p.Type == PlayerType.Human);
            _playerRegistry.RegisterLocalPlayer(localPlayer);
            _playerDirectory.Set(players);
            _matchContext.SetLocalPlayerId(localPlayer.Id);

            _draftFlow.PrepareForDraft(players);
        }

        private void HandleDraftReady(ArenaDraftResult result)
        {
            _matchContext.SetLoadouts(result.LoadoutByPlayerId);
            BeginCombat(_matchSeed, _players, result);
        }

        // ---- shared match build ----

        private void BeginCombat(int matchSeed, List<IPlayer> players, ArenaDraftResult draftResult)
        {
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
            _heroSpawner.SpawnAll(slots, _controller.Battlefield, _controller, draftResult.LoadoutByPlayerId);

            // Seats that dropped during the draft still spawned (identically on every client);
            // the host folds them into round 1's departures so everyone kills them off the bundle.
            if (_session.IsHost || _config.OfflineMode)
            {
                _matchHost.SeedSeats(
                    matchSeed, _matchContext.Roster, _config.DisconnectGraceRounds);
                _matchHost.SeedDeparted(draftResult.DepartedPlayerIds);
            }

            // Every seat is on the board — the last-standing check may go live.
            _controller.ArmWinCondition();

            // X2 anti-cheat: armed on EVERY client (it only runs on whichever machine is the
            // active host — including one promoted by migration).
            if (_config.ValidateCommits)
            {
                _matchHost.EnableValidation(
                    _commitValidator, _queueCredits, _controller, _combatConfig.MaxAbilityQueueSize);
            }

            // P4-3b: per-step simultaneous damage — a rules change, so strictly config-gated
            // (default off keeps the shipped sequential skip-dead resolution).
            if (_config.SimultaneousDamageBatching)
            {
                _controller.EnableStepBatching(_batchEligibility);
            }

            // X1: the reconnect machinery watches the live match (networked play only — the
            // offline loopback has no session to lose).
            if (!_config.OfflineMode)
            {
                if (_session.IsHost)
                {
                    _reconnectHost.Activate();
                }

                _reconnectClient.ArmForMatch();
            }

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

            // On-unit status row (S2): same presenter pair PvE builds in CombatActiveState.
            var statusIconsGo = new GameObject("UnitStatusIconsView");
            _statusIconsView = statusIconsGo.AddComponent<UnitStatusIconsView>();
            _statusIconsView.Initialize(_unitViewRegistry, _statusCatalog);
            _statusIconsPresenter = new UnitStatusIconsPresenter(_controller, _statusIconsView);

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
