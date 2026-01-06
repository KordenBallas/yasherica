using UnityEngine;

namespace Platform
{
    public class PlatformIdleState : PlatformStateBase
    {
        public override void OnEnter(IPlatform platform)
        {
            // Platform is inactive, waiting for player
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} is now idle");
        }
        
        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} exiting idle state");
        }
    }
}

