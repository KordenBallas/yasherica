using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for dialogue interactions.
    /// Placeholder for future dialogue system implementation.
    /// </summary>
    public class DialogueActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<DialogueActiveState> { }

        public override void OnEnter(IPlatform platform)
        {
            Debug.Log($"[DialogueActiveState] Entering dialogue for platform {platform.Id}");
            // TODO: Implement dialogue system
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[DialogueActiveState] Exiting dialogue for platform {platform.Id}");
        }
    }
}
