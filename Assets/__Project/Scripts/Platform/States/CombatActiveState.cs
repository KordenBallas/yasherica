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
        
        private IPlatform _platform;
        private BattlefieldView _battlefieldView;
        
        public CombatActiveState(
            ICombatController controller, 
            ICameraService cameraService,
            CharacterCombatInitializer characterInitializer = null,
            IInputController inputController = null,
            IPlayerRegistry playerRegistry = null,
            ICharacterRegistry characterRegistry = null)
        {
            _controller = controller;
            _cameraService = cameraService;
            _characterInitializer = characterInitializer;
            _inputController = inputController;
            _playerRegistry = playerRegistry;
            _characterRegistry = characterRegistry;
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
                
                // Initialize combat controller with turn system
                if (_playerRegistry != null)
                {
                    IPlayer player = _playerRegistry.GetLocalPlayer();
                    if (player != null)
                    {
                        // Create initial combat state
                        var players = new List<IPlayer> { player };
                        var units = new List<IUnit>(); // Start with empty units, character will be added by initializer
                        var initialState = new CombatState(
                            units, 
                            players, 
                            player,  // currentPlayer
                            1,       // turnNumber
                            CombatPhase.Combat
                        );
                        
                        // Initialize combat controller with turn system
                        _controller.Initialize(initialState, players);
                        Debug.Log($"[CombatActiveState] CombatController initialized with player {player.Id}");
                        
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
                        Debug.LogWarning("[CombatActiveState] Cannot initialize combat: no local player registered");
                    }
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
        
        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[CombatActiveState] Exiting combat active state for platform {platform.Id}");
            
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

