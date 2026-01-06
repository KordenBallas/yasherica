using UnityEngine;

namespace Platform
{
    public class PlatformActiveState : PlatformStateBase
    {
        public override void OnEnter(IPlatform platform)
        {
            // Player is on platform, content is active
            Debug.Log($"[PlatformActiveState] Platform {platform.Id} is now active");
            
            foreach (var content in platform.Contents)
            {
                content.OnPlatformEntered(platform);
            }
        }
        
        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[PlatformActiveState] Platform {platform.Id} exiting active state");
            
            foreach (var content in platform.Contents)
            {
                content.OnPlatformExited(platform);
            }
        }
    }
}

