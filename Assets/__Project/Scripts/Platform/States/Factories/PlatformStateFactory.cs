using System.Linq;
using Core.Logging;
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
        private readonly IGameLogger _logger;

        public PlatformStateFactory(
            PlatformActiveState.Factory activeStateFactory,
            PlatformIdleState.Factory idleStateFactory,
            PlatformCompletedState.Factory completedStateFactory,
            PlatformLockedState.Factory lockedStateFactory,
            CombatActiveState.Factory combatActiveStateFactory,
            CombatIdleState.Factory combatIdleStateFactory,
            DialogueActiveState.Factory dialogueActiveStateFactory,
            CutsceneActiveState.Factory cutsceneActiveStateFactory,
            IGameLogger logger)
        {
            _activeStateFactory = activeStateFactory;
            _idleStateFactory = idleStateFactory;
            _completedStateFactory = completedStateFactory;
            _lockedStateFactory = lockedStateFactory;
            _combatActiveStateFactory = combatActiveStateFactory;
            _combatIdleStateFactory = combatIdleStateFactory;
            _dialogueActiveStateFactory = dialogueActiveStateFactory;
            _cutsceneActiveStateFactory = cutsceneActiveStateFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates the appropriate active state based on platform content.
        /// Priority: Enemy (Combat) > Npc (Dialogue) > Dialogue > Cutscene > Default (Active)
        /// </summary>
        public IPlatformState CreateActiveState(IPlatform platform)
        {
            // Landing never starts a fight: combat is entered by engagement only (an enemy's aggro
            // radius, the camp boss's circle, or a dialogue combat branch — all via the explicit
            // overload below). Activation auto-enters combat only for an already-engaged platform,
            // so re-landing resumes a begun battle (CombatAutoStartRule).
            if (CombatAutoStartRule.ShouldAutoStartCombat(platform.Contents))
            {
                _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating CombatActiveState for platform {platform.Id}");
                return _combatActiveStateFactory.Create();
            }

            // NPC content no longer auto-starts a dialogue on land (NPC Proximity Interaction R4): the
            // player must walk into range and press F (handled by NpcEncounterStarter, which transitions
            // into the dialogue state via CreateDialogueState). Landing on an NPC platform is neutral.

            // Check for pure dialogue content -> Dialogue state
            if (HasContentType(platform, ContentType.Dialogue))
            {
                _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating DialogueActiveState for platform {platform.Id}");
                return _dialogueActiveStateFactory.Create();
            }

            // Check for cutscene content -> Cutscene state
            if (HasContentType(platform, ContentType.Cutscene))
            {
                _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating CutsceneActiveState for platform {platform.Id}");
                return _cutsceneActiveStateFactory.Create();
            }

            // Default: generic active state
            _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating PlatformActiveState for platform {platform.Id}");
            return _activeStateFactory.Create();
        }
        
        /// <summary>
        /// Creates the appropriate active state based on platform content.
        /// </summary>
        public IPlatformState CreateActiveState(IPlatform platform, ContentType contentType)
        {
            // Check for enemy content -> Combat state
            if (ContentType.Enemy.Equals(contentType))
            {
                _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating CombatActiveState for platform {platform.Id}");
                return _combatActiveStateFactory.Create();
            }

            // Default: generic active state
            _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating PlatformActiveState for platform {platform.Id}");
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
                _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating CombatIdleState for platform {platform.Id}");
                return _combatIdleStateFactory.Create();
            }

            // Default: generic idle state
            _logger.Info(LogCategory.Platform,$"[PlatformStateFactory] Creating PlatformIdleState for platform {platform.Id}");
            return _idleStateFactory.Create();
        }

        public IPlatformState CreateDialogueState()
        {
            return _dialogueActiveStateFactory.Create();
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
