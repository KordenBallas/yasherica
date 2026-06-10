using System;
using Narrative.Data.Definitions;
using Narrative.Generation;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Platform content representing an NPC with dialogue capabilities.
    /// Holds an NpcAssignment that pairs the NPC with an optional story.
    /// </summary>
    public class NpcContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Npc;

        /// <summary>
        /// The runtime assignment binding this NPC to a story and rewards.
        /// </summary>
        public NpcAssignment Assignment { get; private set; }

        /// <summary>
        /// Shortcut to the NPC definition.
        /// </summary>
        public NpcDefinition Definition => Assignment?.Npc;

        /// <summary>
        /// Whether this NPC has transitioned to an enemy.
        /// </summary>
        public bool HasTransitionedToEnemy { get; private set; }

        /// <summary>
        /// The spawned NPC GameObject instance.
        /// </summary>
        public GameObject NpcVisual { get; private set; }

        public event Action<NpcContent, EnemyContent> OnTransitionedToEnemy;
        public event Action<NpcContent> OnDialogueStarted;
        public event Action<NpcContent> OnDialogueEnded;

        public NpcContent() { }

        public NpcContent(NpcAssignment assignment)
        {
            Assignment = assignment;
        }

        /// <summary>
        /// Checks if this NPC can transition to an enemy.
        /// </summary>
        public bool CanBecomeEnemy =>
            Definition?.CanBecomeEnemy == true
            && Definition.EnemyDefinition != null
            && !HasTransitionedToEnemy;

        public override void Initialize(IPlatform platform)
        {
            if (Assignment == null)
            {
                Debug.LogWarning($"[NpcContent] No assignment on platform {platform.Id}");
                return;
            }

            SpawnNpcVisual(platform);
        }

        public override void OnPlatformEntered(IPlatform platform)
        {
            if (Definition == null || HasTransitionedToEnemy)
                return;

            OnDialogueStarted?.Invoke(this);
        }

        public override void OnPlatformExited(IPlatform platform)
        {
            OnDialogueEnded?.Invoke(this);
        }

        /// <summary>
        /// Transitions this NPC to an enemy, creating EnemyContent.
        /// </summary>
        public EnemyContent TransitionToEnemy()
        {
            if (!CanBecomeEnemy)
            {
                Debug.LogWarning($"[NpcContent] Cannot transition NPC '{Definition?.NpcId}' to enemy");
                return null;
            }

            HasTransitionedToEnemy = true;

            var enemyContent = new EnemyContent();
            enemyContent.EnemyId = Definition.EnemyDefinition.EnemyId;

            OnTransitionedToEnemy?.Invoke(this, enemyContent);
            return enemyContent;
        }

        public void NotifyDialogueEnded()
        {
            OnDialogueEnded?.Invoke(this);
        }

        /// <summary>
        /// Resets the NPC's combat transition state.
        /// Call this when restarting dialogue or resetting platform state.
        /// </summary>
        public void ResetCombatState()
        {
            HasTransitionedToEnemy = false;
            Debug.Log($"[NpcContent] Combat state reset for NPC '{Definition?.NpcId}'");
        }

        public void DestroyNpcVisual()
        {
            if (NpcVisual != null)
            {
                UnityEngine.Object.Destroy(NpcVisual);
                NpcVisual = null;
            }
        }

        private void SpawnNpcVisual(IPlatform platform)
        {
            if (Definition?.Prefab == null)
            {
                Debug.LogWarning($"[NpcContent] No prefab for NPC '{Definition?.NpcId}'");
                return;
            }

            var spawnPosition = platform.Visual?.Position ?? Vector3.zero;
            NpcVisual = UnityEngine.Object.Instantiate(Definition.Prefab, spawnPosition, Quaternion.identity);
            NpcVisual.name = $"NPC_{Definition.NpcId}";
        }
    }
}
