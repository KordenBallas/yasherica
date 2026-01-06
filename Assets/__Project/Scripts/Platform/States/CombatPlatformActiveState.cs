using Combat.Controller;
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
            }
            else
            {
                Debug.LogWarning($"[CombatPlatformActiveState] Cannot initialize battlefield: invalid platform geometry");
            }
        }
        
        public override void OnExit(IPlatform platform)
        {
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

