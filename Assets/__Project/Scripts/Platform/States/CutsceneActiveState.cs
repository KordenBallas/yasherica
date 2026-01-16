using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for cutscene playback.
    /// Placeholder for future cutscene system implementation.
    /// </summary>
    public class CutsceneActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<CutsceneActiveState> { }

        public override void OnEnter(IPlatform platform)
        {
            Debug.Log($"[CutsceneActiveState] Entering cutscene for platform {platform.Id}");
            // TODO: Implement cutscene system
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[CutsceneActiveState] Exiting cutscene for platform {platform.Id}");
        }
    }
}
