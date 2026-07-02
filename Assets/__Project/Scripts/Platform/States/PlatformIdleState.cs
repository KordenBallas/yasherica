using Core.Logging;
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

        private readonly IGameLogger _logger;

        public PlatformIdleState(IGameLogger logger)
        {
            _logger = logger;
        }

        public override void OnEnter(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform, $"[PlatformIdleState] Platform {platform.Id} is now idle");
        }

        public override void OnExit(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform, $"[PlatformIdleState] Platform {platform.Id} exiting idle state");
        }
    }
}
