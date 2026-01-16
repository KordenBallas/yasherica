using System.Linq;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Factory for creating platform states based on platform content.
    /// Content-driven state selection: EnemyContent->Combat, NpcContent->Dialogue, etc.
    /// </summary>
    public class PlatformStateFactory : IPlatformStateFactory
    {
        private readonly PlatformActiveState.Factory _activeStateFactory;
        private readonly PlatformIdleState.Factory _idleStateFactory;
        private readonly PlatformCompletedState.Factory _completedStateFactory;
        private readonly PlatformLockedState.Factory _lockedStateFactory;
        private readonly CombatActiveState.Factory _combatActiveStateFactory;
        private readonly CombatIdleState.Factory _combatIdleStateFactory;
        private readonly DialogueActiveState.Factory _dialogueActiveStateFactory;
        private readonly CutsceneActiveState.Factory _cutsceneActiveStateFactory;

        public PlatformStateFactory(
            PlatformActiveState.Factory activeStateFactory,
            PlatformIdleState.Factory idleStateFactory,
            PlatformCompletedState.Factory completedStateFactory,
            PlatformLockedState.Factory lockedStateFactory,
            CombatActiveState.Factory combatActiveStateFactory,
            CombatIdleState.Factory combatIdleStateFactory,
            DialogueActiveState.Factory dialogueActiveStateFactory,
            CutsceneActiveState.Factory cutsceneActiveStateFactory)
        {
            _activeStateFactory = activeStateFactory;
            _idleStateFactory = idleStateFactory;
            _completedStateFactory = completedStateFactory;
            _lockedStateFactory = lockedStateFactory;
            _combatActiveStateFactory = combatActiveStateFactory;
            _combatIdleStateFactory = combatIdleStateFactory;
            _dialogueActiveStateFactory = dialogueActiveStateFactory;
            _cutsceneActiveStateFactory = cutsceneActiveStateFactory;
        }

        /// <summary>
        /// Creates the appropriate active state based on platform content.
        /// Priority: Enemy (Combat) > Npc (Dialogue) > Cutscene > Default (Active)
        /// </summary>
        public IPlatformState CreateActiveState(IPlatform platform)
        {
            // Check for enemy content -> Combat state
            if (HasContentType(platform, ContentType.Enemy))
            {
                Debug.Log($"[PlatformStateFactory] Creating CombatActiveState for platform {platform.Id}");
                return _combatActiveStateFactory.Create();
            }

            // Check for NPC content -> Dialogue state
            if (HasContentType(platform, ContentType.Npc))
            {
                Debug.Log($"[PlatformStateFactory] Creating DialogueActiveState for platform {platform.Id}");
                return _dialogueActiveStateFactory.Create();
            }

            // Check for cutscene content -> Cutscene state (using new ContentType)
            // Note: ContentType.Cutscene may need to be added to the enum
            // For now, default to active state

            // Default: generic active state
            Debug.Log($"[PlatformStateFactory] Creating PlatformActiveState for platform {platform.Id}");
            return _activeStateFactory.Create();
        }

        /// <summary>
        /// Creates the appropriate idle state based on platform content.
        /// Platforms with enemy content get CombatIdleState, others get generic idle.
        /// </summary>
        public IPlatformState CreateIdleState(IPlatform platform)
        {
            // Check for enemy content -> Combat idle state (handles enemy spawning)
            if (HasContentType(platform, ContentType.Enemy))
            {
                Debug.Log($"[PlatformStateFactory] Creating CombatIdleState for platform {platform.Id}");
                return _combatIdleStateFactory.Create();
            }

            // Default: generic idle state
            Debug.Log($"[PlatformStateFactory] Creating PlatformIdleState for platform {platform.Id}");
            return _idleStateFactory.Create();
        }

        public IPlatformState CreateCompletedState()
        {
            return _completedStateFactory.Create();
        }

        public IPlatformState CreateLockedState()
        {
            return _lockedStateFactory.Create();
        }

        private bool HasContentType(IPlatform platform, ContentType type)
        {
            return platform.Contents.Any(c => c.Type == type);
        }
    }
}
