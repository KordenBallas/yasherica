using Character;
using Combat.Controller;
using Combat.Battlefield;
using Combat.Core;
using Combat.Integration;
using Combat.Input;
using Combat.Player;
using Core.Camera;
using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Active combat state that handles battlefield initialization and camera transitions.
    /// Initializes the combat battlefield when entered and cleans up when exited.
    /// Manages camera switching between isometric and combat views.
    /// Now includes character combat initialization following DIP principles.
    /// </summary>
    public class CombatActiveState : PlatformStateBase
    {
        private readonly ICombatController _controller;
        private readonly ICameraService _cameraService;
        private readonly CharacterCombatInitializer _characterInitializer;
        private readonly IInputController _inputController;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly EnemyCombatIntegrator _enemyIntegrator;
        private readonly AITurnController _aiTurnController;

        private IPlatform _platform;
        private BattlefieldView _battlefieldView;

        public CombatActiveState(
            ICombatController controller,
            ICameraService cameraService,
            CharacterCombatInitializer characterInitializer = null,
            IInputController inputController = null,
            IPlayerRegistry playerRegistry = null,
            ICharacterRegistry characterRegistry = null,
            EnemyCombatIntegrator enemyIntegrator = null,
            AITurnController aiTurnController = null)
        {
            _controller = controller;
            _cameraService = cameraService;
            _characterInitializer = characterInitializer;
            _inputController = inputController;
            _playerRegistry = playerRegistry;
            _characterRegistry = characterRegistry;
            _enemyIntegrator = enemyIntegrator;
            _aiTurnController = aiTurnController;
        }
        
        public override void OnEnter(IPlatform platform)
        {
            _platform = platform;
            
            Debug.Log($"[CombatActiveState] Entering combat active state for platform {platform.Id}");
            
            // Initialize combat battlefield with platform geometry
            if (platform.Visual?.TopBoundary != null && platform.Visual.TopBoundary.Count > 0)
            {
                _controller.InitializeBattlefield(
                    platform.Visual.TopBoundary,
                    platform.Visual.Position);
                    
                Debug.Log($"[CombatActiveState] Initialized battlefield for platform {platform.Id}");
                
                // Create and initialize BattlefieldView for visualization
                InitializeBattlefieldView(platform);

                // Initialize AI turn controller BEFORE initializing combat state
                // This ensures it's subscribed to OnTurnStarted before first turn begins
                // CRITICAL: Must pass the actual controller instance, not a DI singleton
                if (_aiTurnController != null && platform.Visual?.GameObject != null)
                {
                    var mono = platform.Visual.GameObject.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                        _aiTurnController.Initialize(_controller, mono);
                        Debug.Log("[CombatActiveState] AITurnController initialized with correct controller instance (before combat start)");
                    }
                    else
                    {
                        Debug.LogWarning("[CombatActiveState] Cannot initialize AITurnController: no MonoBehaviour on platform");
                    }
                }
                else if (_aiTurnController == null)
                {
                    Debug.LogWarning("[CombatActiveState] AITurnController not provided - AI turns will not work!");
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
                        Debug.LogWarning("[CombatActiveState] No local player registered");
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
                        Debug.Log($"[CombatActiveState] Added enemy player {enemyContent.EnemyPlayer.Name} (ID: {enemyContent.EnemyPlayer.Id}) to turn order");
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
                        CombatPhase.Combat
                    );

                    // Initialize combat controller with turn system
                    _controller.Initialize(initialState, allPlayers);
                    Debug.Log($"[CombatActiveState] CombatController initialized with {allPlayers.Count} players");

                    // Verify turn manager state
                    if (_controller.TurnManager != null)
                    {
                        Debug.Log($"[CombatActiveState] Turn system ready - Current Player: {_controller.TurnManager.CurrentPlayer?.Id ?? -1}, Turn: {_controller.TurnManager.CurrentTurnNumber}");
                    }
                    else
                    {
                        Debug.LogWarning("[CombatActiveState] TurnManager is null after initialization!");
                    }
                }
                else
                {
                    Debug.LogWarning("[CombatActiveState] Cannot initialize combat: no players available");
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
                                Debug.Log("[CombatActiveState] Started character combat initialization");
                            }
                            else
                            {
                                Debug.LogWarning("[CombatActiveState] Cannot start coroutine: no MonoBehaviour on platform");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[CombatActiveState] Cannot initialize character: player or battlefield is null");
                    }
                }
                else
                {
                    Debug.LogWarning("[CombatActiveState] Character combat system dependencies not provided");
                }

                // Initialize enemies for combat
                if (_enemyIntegrator != null && platform.Visual?.GameObject != null)
                {
                    var mono = platform.Visual.GameObject.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                        mono.StartCoroutine(InitializeEnemiesForCombat(platform));
                        Debug.Log("[CombatActiveState] Started enemy combat initialization");
                    }
                    else
                    {
                        Debug.LogWarning("[CombatActiveState] Cannot start enemy integration: no MonoBehaviour on platform");
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
                Debug.LogWarning($"[CombatActiveState] Cannot initialize battlefield: invalid platform geometry");
            }
        }
        
        private void InitializeBattlefieldView(IPlatform platform)
        {
            // Get platform GameObject from Visual
            if (platform.Visual?.GameObject == null)
            {
                Debug.LogWarning($"[CombatActiveState] Cannot create BattlefieldView: platform GameObject is null");
                return;
            }
            
            var platformGO = platform.Visual.GameObject;
            
            // Get or add BattlefieldView component
            var battlefieldView = platformGO.GetComponent<BattlefieldView>();
            if (battlefieldView == null)
            {
                battlefieldView = platformGO.AddComponent<BattlefieldView>();
                Debug.Log($"[CombatActiveState] Added BattlefieldView component to platform {platform.Id}");
            }
            
            // Initialize view with battlefield
            if (_controller.Battlefield != null)
            {
                battlefieldView.Initialize(_controller.Battlefield);
                _battlefieldView = battlefieldView;
                Debug.Log($"[CombatActiveState] BattlefieldView initialized and hex grid should be visible");
            }
            else
            {
                Debug.LogWarning($"[CombatActiveState] Cannot initialize BattlefieldView: controller battlefield is null");
            }
        }

        /// <summary>
        /// Coroutine that integrates all alive enemies into combat.
        /// Finds closest hex cell for each enemy and adds them to combat state.
        /// </summary>
        private System.Collections.IEnumerator InitializeEnemiesForCombat(IPlatform platform)
        {
            Debug.Log("[CombatActiveState] Initializing enemies for combat");

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
                    Debug.Log($"[CombatActiveState] Enemy {enemyContent.EnemyId} integrated into combat");
                }
            }

            Debug.Log($"[CombatActiveState] Enemy integration complete: {enemyCount} enemies added to combat");
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[CombatActiveState] Exiting combat active state for platform {platform.Id}");

            // Dispose AI turn controller if provided
            if (_aiTurnController != null)
            {
                _aiTurnController.Dispose();
                Debug.Log("[CombatActiveState] AITurnController disposed");
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
                        Debug.Log("[CombatActiveState] Re-enabled CharacterMovementController");
                    }

                    // Destroy CharacterCombatCoordinator
                    var coordinator = character.GetComponent<CharacterCombatCoordinator>();
                    if (coordinator != null)
                    {
                        Object.Destroy(coordinator);
                        Debug.Log("[CombatActiveState] Destroyed CharacterCombatCoordinator");
                    }

                    // Note: We don't destroy CharacterCombatComponent as it may be reused
                    // It will be reinitialized on next combat entry

                    Debug.Log("[CombatActiveState] Character combat cleanup complete");
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
                                Debug.Log($"[CombatActiveState] Restored enemy {enemyContent.EnemyId} Rigidbody to non-kinematic");
                            }
                        }
                        // If enemy is dead, destroy the GameObject
                        else
                        {
                            Object.Destroy(enemyComponent.gameObject);
                            Debug.Log($"[CombatActiveState] Destroyed dead enemy {enemyContent.EnemyId}");
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
                Debug.Log($"[CombatActiveState] Destroyed BattlefieldView for platform {_platform?.Id}");
            }
            
            // Cleanup combat when leaving platform
            _controller.CleanupBattlefield();
            Debug.Log($"[CombatActiveState] Cleaned up battlefield for platform {_platform?.Id}");
            
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

