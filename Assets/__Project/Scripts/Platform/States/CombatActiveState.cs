using Character;
using Combat.Controller;
using Combat.Battlefield;
using Combat.Core;
using Combat.Data;
using Combat.Integration;
using Combat.Input;
using Combat.Player;
using Core.Camera;
using Core.Logging;
using Narrative.Dialogue;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active combat state that handles battlefield initialization and camera transitions.
    /// Initializes the combat battlefield when entered and cleans up when exited.
    /// Manages camera switching between isometric and combat views.
    /// Now includes character combat initialization following DIP principles.
    /// Creates and manages ICombatController lifecycle.
    /// </summary>
    public class CombatActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<CombatActiveState> { }

        private readonly IFactory<ICombatController> _controllerFactory;
        private readonly ICameraService _cameraService;
        private readonly CharacterCombatInitializer _characterInitializer;
        private readonly IInputController _inputController;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly EnemyCombatIntegrator _enemyIntegrator;
        private readonly AITurnController _aiTurnController;
        private readonly IEnemyDataProvider _enemyDataProvider;
        private readonly CombatActivityTracker _combatActivityTracker;
        private readonly Loot.Application.IEnemyLootDropper _enemyLootDropper;
        private readonly DialogueRunner _dialogueRunner;
        private readonly IGameLogger _logger;

        private IPlatform _platform;
        private ICombatController _controller;
        private BattlefieldView _battlefieldView;

        public CombatActiveState(
            IFactory<ICombatController> controllerFactory,
            ICameraService cameraService,
            CharacterCombatInitializer characterInitializer,
            IInputController inputController,
            IPlayerRegistry playerRegistry,
            ICharacterRegistry characterRegistry,
            EnemyCombatIntegrator enemyIntegrator,
            AITurnController aiTurnController,
            IEnemyDataProvider enemyDataProvider,
            CombatActivityTracker combatActivityTracker,
            Loot.Application.IEnemyLootDropper enemyLootDropper,
            DialogueRunner dialogueRunner,
            IGameLogger logger)
        {
            _controllerFactory = controllerFactory;
            _cameraService = cameraService;
            _characterInitializer = characterInitializer;
            _inputController = inputController;
            _playerRegistry = playerRegistry;
            _characterRegistry = characterRegistry;
            _enemyIntegrator = enemyIntegrator;
            _aiTurnController = aiTurnController;
            _enemyDataProvider = enemyDataProvider;
            _combatActivityTracker = combatActivityTracker;
            _enemyLootDropper = enemyLootDropper;
            _dialogueRunner = dialogueRunner;
            _logger = logger;
        }

        public override void OnEnter(IPlatform platform)
        {
            _platform = platform;

            _combatActivityTracker?.SetCombatActive(true);

            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Entering combat active state for platform {platform.Id}");

            // Get existing controller from platform or create new one
            _controller = platform.GetCombatController();
            if (_controller == null)
            {
                _controller = _controllerFactory.Create();
                platform.SetCombatController(_controller);
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Created new CombatController for platform {platform.Id}");
            }

            // Subscribe to combat end event
            _controller.OnGameEnded += HandleCombatEnded;

            // Initialize combat battlefield from the platform's hex surface — the grid IS the ground
            // tiles, never re-fitted (brief §1).
            if (platform.Visual?.Surface != null && platform.Visual.Surface.Cells.Count > 0)
            {
                _controller.InitializeBattlefield(
                    platform.Visual.Surface,
                    platform.Visual.Position);

                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Initialized battlefield for platform {platform.Id}");

                // Create and initialize BattlefieldView for visualization
                InitializeBattlefieldView(platform);

                // Instantiate any uninstantiated enemies (e.g., from NPC transitions)
                // MUST happen BEFORE collecting players
                InstantiateUninstantiatedEnemies(platform);

                // Initialize AI turn controller BEFORE initializing combat state
                // This ensures it's subscribed to OnTurnStarted before first turn begins
                // CRITICAL: Must pass the actual controller instance, not a DI singleton
                if (_aiTurnController != null && platform.Visual?.GameObject != null)
                {
                    var mono = platform.Visual.GameObject.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                        _aiTurnController.Initialize(_controller, mono);
                        _logger.Info(LogCategory.Platform,"[CombatActiveState] AITurnController initialized with correct controller instance (before combat start)");
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Platform,"[CombatActiveState] Cannot initialize AITurnController: no MonoBehaviour on platform");
                    }
                }
                else if (_aiTurnController == null)
                {
                    _logger.Warning(LogCategory.Platform,"[CombatActiveState] AITurnController not provided - AI turns will not work!");
                }

                // Collect all players (human + AI enemies)
                List<IPlayer> allPlayers = new List<IPlayer>();

                // Add local player
                if (_playerRegistry != null)
                {
                    IPlayer player = _playerRegistry.GetLocalPlayer();
                    if (player != null)
                    {
                        allPlayers.Add(player);
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Platform,"[CombatActiveState] No local player registered");
                    }
                }

                // Add enemy AI players from platform content
                foreach (var content in platform.Contents)
                {
                    if (content is EnemyContent enemyContent &&
                        enemyContent.HasBeenInstantiated &&
                        enemyContent.EnemyPlayer != null)
                    {
                        allPlayers.Add(enemyContent.EnemyPlayer);
                        _logger.Info(LogCategory.Platform,$"[CombatActiveState] Added enemy player {enemyContent.EnemyPlayer.Name} (ID: {enemyContent.EnemyPlayer.Id}) to turn order");
                    }
                }

                // Initialize combat controller with all players
                if (allPlayers.Count > 0)
                {
                    // Create initial combat state
                    var units = new List<IUnit>(); // Units will be added by character and enemy initializers
                    var initialState = new CombatState(
                        units,
                        allPlayers,
                        allPlayers[0],  // First player starts
                        1,              // turnNumber
                        CombatPhase.Combat,
                        null            // Battlefield already initialized, will be injected next
                    );

                    // Initialize combat controller with turn system
                    _controller.Initialize(initialState, allPlayers);
                    _logger.Info(LogCategory.Platform,$"[CombatActiveState] CombatController initialized with {allPlayers.Count} players");

                    // Verify turn manager state
                    if (_controller.TurnManager != null)
                    {
                        _logger.Info(LogCategory.Platform,$"[CombatActiveState] Turn system ready - Current Player: {_controller.TurnManager.CurrentPlayer?.Id ?? -1}, Turn: {_controller.TurnManager.CurrentTurnNumber}");
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Platform,"[CombatActiveState] TurnManager is null after initialization!");
                    }
                }
                else
                {
                    _logger.Warning(LogCategory.Platform,"[CombatActiveState] Cannot initialize combat: no players available");
                }

                // Initialize character for combat
                if (_characterInitializer != null && _playerRegistry != null)
                {
                    IPlayer player = _playerRegistry.GetLocalPlayer();
                    if (player != null && _controller.Battlefield != null)
                    {
                        // Start coroutine for character initialization
                        if (platform.Visual?.GameObject != null)
                        {
                            var mono = platform.Visual.GameObject.GetComponent<MonoBehaviour>();
                            if (mono != null)
                            {
                                mono.StartCoroutine(_characterInitializer.InitializeCharacterForCombat(
                                    player,
                                    _controller.Battlefield,
                                    _controller));
                                _logger.Info(LogCategory.Platform,"[CombatActiveState] Started character combat initialization");
                            }
                            else
                            {
                                _logger.Warning(LogCategory.Platform,"[CombatActiveState] Cannot start coroutine: no MonoBehaviour on platform");
                            }
                        }
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Platform,"[CombatActiveState] Cannot initialize character: player or battlefield is null");
                    }
                }
                else
                {
                    _logger.Warning(LogCategory.Platform,"[CombatActiveState] Character combat system dependencies not provided");
                }

                // Initialize enemies for combat
                if (_enemyIntegrator != null && platform.Visual?.GameObject != null)
                {
                    var mono = platform.Visual.GameObject.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                        mono.StartCoroutine(InitializeEnemiesForCombat(platform));
                        _logger.Info(LogCategory.Platform,"[CombatActiveState] Started enemy combat initialization");
                    }
                    else
                    {
                        _logger.Warning(LogCategory.Platform,"[CombatActiveState] Cannot start enemy integration: no MonoBehaviour on platform");
                    }
                }

                // Switch to combat camera with smooth transition
                _cameraService.SwitchToCombatCamera();

                // Enable input controller if provided
                if (_inputController != null)
                {
                    _inputController.Enable();
                }
            }
            else
            {
                _logger.Warning(LogCategory.Platform,$"[CombatActiveState] Cannot initialize battlefield: invalid platform geometry");
            }
        }

        private void HandleCombatEnded(IPlayer winner, CombatPhase phase)
        {
            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Combat ended - Phase: {phase}, Winner: {winner?.Name ?? "None"}");

            // Drop loot BEFORE the state change: OnExit destroys dead enemy
            // GameObjects, losing their death positions.
            bool playerWon = winner != null && winner.Id == _playerRegistry.GetLocalPlayer()?.Id;
            _enemyLootDropper.DropFor(_platform, playerWon);

            // If this combat was triggered mid-dialogue, the runner is suspended waiting for the result;
            // feed it back so post-combat lines/facts (e.g. a win clearing a path) replay (R7) before the
            // platform completes.
            if (_dialogueRunner != null && _dialogueRunner.State == DialogueRunnerState.AwaitingExternal)
            {
                _dialogueRunner.ReportCombatResult(playerWon);
            }

            // Use platform's state factory to create completed state
            var completedState = _platform.StateFactory.CreateCompletedState();
            _platform.StateMachine.ChangeState(completedState);
        }

        private void InitializeBattlefieldView(IPlatform platform)
        {
            // Get platform GameObject from Visual
            if (platform.Visual?.GameObject == null)
            {
                _logger.Warning(LogCategory.Platform,$"[CombatActiveState] Cannot create BattlefieldView: platform GameObject is null");
                return;
            }

            var platformGO = platform.Visual.GameObject;

            // Get or add BattlefieldView component
            var battlefieldView = platformGO.GetComponent<BattlefieldView>();
            if (battlefieldView == null)
            {
                battlefieldView = platformGO.AddComponent<BattlefieldView>();
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Added BattlefieldView component to platform {platform.Id}");
            }

            // Initialize view with battlefield
            if (_controller.Battlefield != null)
            {
                battlefieldView.Initialize(_controller.Battlefield);
                _battlefieldView = battlefieldView;
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] BattlefieldView initialized and hex grid should be visible");
            }
            else
            {
                _logger.Warning(LogCategory.Platform,$"[CombatActiveState] Cannot initialize BattlefieldView: controller battlefield is null");
            }
        }

        /// <summary>
        /// Instantiates any enemies that haven't been instantiated yet.
        /// This handles NPC-to-enemy transitions where EnemyContent is created
        /// but not yet instantiated as a GameObject.
        /// </summary>
        private void InstantiateUninstantiatedEnemies(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform,"[CombatActiveState] Checking for uninstantiated enemies");

            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent && !enemyContent.HasBeenInstantiated)
                {
                    InstantiateEnemy(platform, enemyContent);
                }
            }
        }

        /// <summary>
        /// Instantiates a single enemy from EnemyContent.
        /// Creates AIPlayer, spawns GameObject, and initializes combat component.
        /// </summary>
        private void InstantiateEnemy(IPlatform platform, EnemyContent enemyContent)
        {
            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Instantiating enemy {enemyContent.EnemyId} from NPC transition");

            // Get enemy data
            var enemyData = _enemyDataProvider.GetEnemyData(enemyContent.EnemyId);
            if (enemyData == null)
            {
                _logger.Error(LogCategory.Platform,$"[CombatActiveState] Enemy data not found for enemy {enemyContent.EnemyId}");
                return;
            }

            // Create AIPlayer for this enemy
            var enemyPlayer = _enemyIntegrator.CreateEnemyPlayer(enemyContent.EnemyId, enemyData);

            // Load enemy prefab - prefer EnemyDefinition.Prefab, fallback to Resources
            GameObject enemyPrefab = enemyData.Prefab;
            if (enemyPrefab == null)
            {
                enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
                if (enemyPrefab == null)
                {
                    _logger.Error(LogCategory.Platform,$"[CombatActiveState] Enemy prefab not found for enemy {enemyContent.EnemyId}");
                    return;
                }
            }

            // Instantiate enemy above platform center (will fall via gravity to surface)
            const float spawnHeightOffset = 2f;
            Vector3 spawnPosition = platform.Visual.Position + Vector3.up * spawnHeightOffset;
            GameObject enemyGO = Object.Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemyGO.name = $"Enemy_{enemyContent.EnemyId}";

            // Get or add combat component
            var combatComponent = enemyGO.GetComponent<Combat.Enemy.EnemyCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = enemyGO.AddComponent<Combat.Enemy.EnemyCombatComponent>();
            }

            // Add Rigidbody for gravity simulation if not present
            var rb = enemyGO.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemyGO.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Added Rigidbody to enemy {enemyContent.EnemyId} for gravity simulation");
            }

            // Store in content
            enemyContent.InstantiateEnemy(enemyPlayer, combatComponent);

            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Enemy {enemyContent.EnemyId} instantiated on platform {platform.Id}");
        }

        /// <summary>
        /// Coroutine that integrates all alive enemies into combat.
        /// Finds closest hex cell for each enemy and adds them to combat state.
        /// </summary>
        private System.Collections.IEnumerator InitializeEnemiesForCombat(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform,"[CombatActiveState] Initializing enemies for combat");

            int enemyCount = 0;
            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent &&
                    enemyContent.HasBeenInstantiated &&
                    enemyContent.EnemyCombatComponent != null &&
                    !enemyContent.EnemyCombatComponent.IsInitializedForCombat)
                {
                    yield return _enemyIntegrator.IntegrateEnemyForCombat(
                        enemyContent.EnemyId,
                        enemyContent.EnemyPlayer,
                        enemyContent.EnemyCombatComponent,  // Pass existing component
                        _controller.Battlefield,
                        _controller,
                        platform.Visual.Position);

                    enemyCount++;
                    _logger.Info(LogCategory.Platform,$"[CombatActiveState] Enemy {enemyContent.EnemyId} integrated into combat");
                }
            }

            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Enemy integration complete: {enemyCount} enemies added to combat");
        }

        public override void OnExit(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Exiting combat active state for platform {platform.Id}");

            _combatActivityTracker?.SetCombatActive(false);

            // Unsubscribe from combat end event
            if (_controller != null)
            {
                _controller.OnGameEnded -= HandleCombatEnded;
            }

            // Dispose AI turn controller if provided
            if (_aiTurnController != null)
            {
                _aiTurnController.Dispose();
                _logger.Info(LogCategory.Platform,"[CombatActiveState] AITurnController disposed");
            }

            // Disable input controller if provided
            if (_inputController != null)
            {
                _inputController.Disable();
            }

            // Cleanup character combat mode
            if (_characterRegistry != null)
            {
                Transform character = _characterRegistry.GetPlayerCharacter();
                if (character != null)
                {
                    // Re-enable CharacterMovementController
                    var movementController = character.GetComponent<CharacterMovementController>();
                    if (movementController != null)
                    {
                        movementController.enabled = true;
                        _logger.Info(LogCategory.Platform,"[CombatActiveState] Re-enabled CharacterMovementController");
                    }

                    // Destroy CharacterCombatCoordinator
                    var coordinator = character.GetComponent<CharacterCombatCoordinator>();
                    if (coordinator != null)
                    {
                        Object.Destroy(coordinator);
                        _logger.Info(LogCategory.Platform,"[CombatActiveState] Destroyed CharacterCombatCoordinator");
                    }

                    // Note: We don't destroy CharacterCombatComponent as it may be reused
                    // It will be reinitialized on next combat entry

                    _logger.Info(LogCategory.Platform,"[CombatActiveState] Character combat cleanup complete");
                }
            }

            // Cleanup enemy combat mode - restore physics for alive enemies
            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent && enemyContent.HasBeenInstantiated)
                {
                    var enemyComponent = enemyContent.EnemyCombatComponent;
                    if (enemyComponent != null)
                    {
                        // If enemy is alive, restore non-kinematic Rigidbody for gravity
                        if (enemyContent.IsEnemyAlive)
                        {
                            var rb = enemyComponent.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.isKinematic = false;
                                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Restored enemy {enemyContent.EnemyId} Rigidbody to non-kinematic");
                            }
                        }
                        // If enemy is dead, destroy the GameObject
                        else
                        {
                            Object.Destroy(enemyComponent.gameObject);
                            _logger.Info(LogCategory.Platform,$"[CombatActiveState] Destroyed dead enemy {enemyContent.EnemyId}");
                        }
                    }
                }
            }

            // Cleanup battlefield view
            if (_battlefieldView != null)
            {
                _battlefieldView.Clear();
                Object.Destroy(_battlefieldView);
                _battlefieldView = null;
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Destroyed BattlefieldView for platform {_platform?.Id}");
            }

            // Cleanup combat when leaving platform
            if (_controller != null)
            {
                _controller.CleanupBattlefield();
                _logger.Info(LogCategory.Platform,$"[CombatActiveState] Cleaned up battlefield for platform {_platform?.Id}");
            }

            // Revert to isometric camera with smooth transition
            _cameraService.SwitchToIsometricCamera();

            _platform = null;
        }

        public override void OnUpdate(IPlatform platform)
        {
            // Combat update logic if needed
            // For now, CombatController.Update() is called separately
        }
    }
}
