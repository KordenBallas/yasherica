using Combat.Controller;
using Combat.Battlefield;
using Core.Camera;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Active combat state that handles battlefield initialization and camera transitions.
    /// Initializes the combat battlefield when entered and cleans up when exited.
    /// Manages camera switching between isometric and combat views.
    /// </summary>
    public class CombatActiveState : PlatformStateBase
    {
        private readonly ICombatController _controller;
        private readonly ICameraService _cameraService;
        private IPlatform _platform;
        private BattlefieldView _battlefieldView;
        
        public CombatActiveState(ICombatController controller, ICameraService cameraService)
        {
            _controller = controller;
            _cameraService = cameraService;
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
                
                // Switch to combat camera with smooth transition
                _cameraService.SwitchToCombatCamera();
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

