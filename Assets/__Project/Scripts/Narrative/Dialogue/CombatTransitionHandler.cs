using Platform;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Default implementation of ICombatTransitionHandler.
    /// Handles NPC-to-enemy transitions and combat state changes.
    /// </summary>
    public class CombatTransitionHandler : ICombatTransitionHandler
    {
        private readonly IPlatformStateFactory _stateFactory;

        public CombatTransitionHandler(IPlatformStateFactory stateFactory)
        {
            _stateFactory = stateFactory;
        }

        public bool CanTransitionToCombat(IPlatform platform, IDialogueContext context, bool combatTriggered)
        {
            if (platform == null)
                return false;

            // Combat can be triggered if:
            // 1. NPC can become enemy
            // 2. Combat was explicitly triggered via Ink function
            if (combatTriggered)
            {
                Debug.Log("[CombatTransitionHandler] Combat explicitly triggered");
                return true;
            }

            // Check if context has an NPC that can become enemy
            if (context?.BoundNpcInstance?.Definition?.CanBecomeEnemy == true)
            {
                Debug.Log("[CombatTransitionHandler] NPC can become enemy");
                return true;
            }

            return false;
        }

        public EnemyContent PrepareEnemyContent(NpcContent npcContent)
        {
            if (npcContent == null)
            {
                Debug.LogWarning("[CombatTransitionHandler] Cannot prepare enemy content: npcContent is null");
                return null;
            }

            if (!npcContent.CanBecomeEnemy)
            {
                Debug.Log($"[CombatTransitionHandler] NPC '{npcContent.Definition?.NpcId}' cannot become enemy");
                return null;
            }

            // Transition the NPC to enemy
            var enemyContent = npcContent.TransitionToEnemy();
            if (enemyContent != null)
            {
                Debug.Log($"[CombatTransitionHandler] Prepared enemy content from NPC '{npcContent.Definition?.NpcId}'");
            }

            return enemyContent;
        }

        public void ExecuteTransition(IPlatform platform, NpcContent npcContent, EnemyContent enemyContent)
        {
            if (platform == null)
            {
                Debug.LogWarning("[CombatTransitionHandler] Cannot execute transition: platform is null");
                return;
            }

            // Add enemy content to platform if we created it
            if (enemyContent != null)
            {
                platform.AddContent(enemyContent);
                Debug.Log("[CombatTransitionHandler] Added enemy content to platform");
            }

            // Destroy NPC visual if applicable
            if (npcContent != null)
            {
                npcContent.DestroyNpcVisual();
            }

            // Create and transition to combat state
            var combatState = _stateFactory.CreateActiveState(platform, ContentType.Enemy);
            platform.TransitionToState(combatState);
            Debug.Log("[CombatTransitionHandler] Transitioned platform to combat state");
        }
    }
}
