using Combat.Controller;
using Combat.Battlefield;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Active state for Combat platforms.
    /// Handles Combat system initialization when platform is entered
    /// and cleanup when platform is exited.
    /// </summary>
    public class CombatPlatformActiveState : PlatformStateBase
    {
        private readonly ICombatController _controller;
        private IPlatform _platform;
        private BattlefieldView _battlefieldView;
        
        public CombatPlatformActiveState(ICombatController controller)
        {
            _controller = controller;
        }
        
        public override void OnEnter(IPlatform platform)
        {
            _platform = platform;
            
            // Initialize combat battlefield with platform geometry
            if (platform.Visual?.TopBoundary != null && platform.Visual.TopBoundary.Count > 0)
            {
                _controller.InitializeBattlefield(
                    platform.Visual.TopBoundary,
                    platform.Visual.Position);
                    
                Debug.Log($"[CombatPlatformActiveState] Initialized battlefield for platform {platform.Id}");
                
                // Create and initialize BattlefieldView for visualization
                InitializeBattlefieldView(platform);
            }
            else
            {
                Debug.LogWarning($"[CombatPlatformActiveState] Cannot initialize battlefield: invalid platform geometry");
            }
        }
        
        private void InitializeBattlefieldView(IPlatform platform)
        {
            // Get platform GameObject from Visual
            if (platform.Visual?.GameObject == null)
            {
                Debug.LogWarning($"[CombatPlatformActiveState] Cannot create BattlefieldView: platform GameObject is null");
                return;
            }
            
            var platformGO = platform.Visual.GameObject;
            
            // Get or add BattlefieldView component
            var battlefieldView = platformGO.GetComponent<BattlefieldView>();
            if (battlefieldView == null)
            {
                battlefieldView = platformGO.AddComponent<BattlefieldView>();
                Debug.Log($"[CombatPlatformActiveState] Added BattlefieldView component to platform {platform.Id}");
            }
            
            // Initialize view with battlefield
            if (_controller.Battlefield != null)
            {
                battlefieldView.Initialize(_controller.Battlefield);
                _battlefieldView = battlefieldView;
                Debug.Log($"[CombatPlatformActiveState] BattlefieldView initialized and hex grid should be visible");
            }
            else
            {
                Debug.LogWarning($"[CombatPlatformActiveState] Cannot initialize BattlefieldView: controller battlefield is null");
            }
        }
        
        public override void OnExit(IPlatform platform)
        {
            // Cleanup battlefield view
            if (_battlefieldView != null)
            {
                _battlefieldView.Clear();
                Object.Destroy(_battlefieldView);
                _battlefieldView = null;
                Debug.Log($"[CombatPlatformActiveState] Destroyed BattlefieldView for platform {_platform?.Id}");
            }
            
            // Cleanup combat when leaving platform
            _controller.CleanupBattlefield();
            Debug.Log($"[CombatPlatformActiveState] Cleaned up battlefield for platform {_platform?.Id}");
        }
        
        public override void OnUpdate(IPlatform platform)
        {
            // Combat update logic can go here if needed
            // For now, CombatController.Update() is called separately
        }
    }
}

