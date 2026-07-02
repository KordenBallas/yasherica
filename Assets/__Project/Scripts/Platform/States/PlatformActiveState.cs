using Core.Logging;
using UnityEngine;
using Zenject;

namespace Platform
{
    public class PlatformActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<PlatformActiveState> { }

        private readonly IGameLogger _logger;

        public PlatformActiveState(IGameLogger logger)
        {
            _logger = logger;
        }

        public override void OnEnter(IPlatform platform)
        {
            // Player is on platform, content is active
            _logger.Info(LogCategory.Platform, $"[PlatformActiveState] Platform {platform.Id} is now active");
            
            foreach (var content in platform.Contents)
            {
                content.OnPlatformEntered(platform);
            }
        }
        
        public override void OnExit(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform, $"[PlatformActiveState] Platform {platform.Id} exiting active state");
            
            foreach (var content in platform.Contents)
            {
                content.OnPlatformExited(platform);
            }
        }
    }
}

