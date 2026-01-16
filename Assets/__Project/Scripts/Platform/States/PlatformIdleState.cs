using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Generic idle state for platforms without combat content.
    /// Combat-specific idle behavior is handled by CombatIdleState.
    /// </summary>
    public class PlatformIdleState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<PlatformIdleState> { }

        public override void OnEnter(IPlatform platform)
        {
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} is now idle");
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} exiting idle state");
        }
    }
}
