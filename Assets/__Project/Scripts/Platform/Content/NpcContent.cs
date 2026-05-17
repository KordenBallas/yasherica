using System;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Generation;
using Narrative.Providers;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Platform content representing an NPC with dialogue capabilities.
    /// Supports transition to enemy for combat scenarios.
    /// Enhanced with NpcStoryProvider for dynamic story selection.
    /// </summary>
    public class NpcContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Npc;

        /// <summary>
        /// NPC definition containing identity, visual, and behavior data.
        /// </summary>
        public NpcDefinition Definition { get; private set; }

        /// <summary>
        /// Optional dialogue session for this encounter (legacy).
        /// </summary>
        public DialogueSessionDefinition DialogueSession { get; private set; }

        /// <summary>
        /// The story selected for this NPC encounter (new system).
        /// </summary>
        public BaseStoryDefinition SelectedStory { get; private set; }

        /// <summary>
        /// Runtime NpcInstance with bound state, role, and relationships.
        /// Set by NpcContentInstanceBinder when using procedural generation.
        /// </summary>
        public NpcInstance RuntimeInstance { get; private set; }

        /// <summary>
        /// The Ink knot to use for this NPC's dialogue.
        /// Falls back to Definition.DefaultDialogueKnot if not set.
        /// </summary>
        public string DialogueKnot { get; set; }

        // Optional dependencies for dynamic story selection
        [Inject(Optional = true)] private INpcStoryProvider _npcStoryProvider;
        [Inject(Optional = true)] private IStoryStateProvider _storyStateProvider;
        [Inject(Optional = true)] private INpcPool _npcPool;

        /// <summary>
        /// Whether this NPC has transitioned to an enemy.
        /// </summary>
        public bool HasTransitionedToEnemy { get; private set; }

        /// <summary>
        /// The spawned NPC GameObject instance.
        /// </summary>
        public GameObject NpcInstance { get; private set; }

        /// <summary>
        /// Event fired when the NPC transitions to an enemy.
        /// </summary>
        public event Action<NpcContent, EnemyContent> OnTransitionedToEnemy;

        /// <summary>
        /// Event fired when dialogue starts with this NPC.
        /// </summary>
        public event Action<NpcContent> OnDialogueStarted;

        /// <summary>
        /// Event fired when dialogue ends with this NPC.
        /// </summary>
        public event Action<NpcContent> OnDialogueEnded;

        public NpcContent()
        {
        }

        public NpcContent(NpcDefinition definition, DialogueSessionDefinition dialogueSession = null)
        {
            Definition = definition;
            DialogueSession = dialogueSession;
            DialogueKnot = dialogueSession?.FullInkPath ?? definition?.DefaultDialogueKnot;
        }

        /// <summary>
        /// Gets the effective dialogue knot path.
        /// </summary>
        public string EffectiveDialogueKnot =>
            !string.IsNullOrEmpty(DialogueKnot)
                ? DialogueKnot
                : Definition?.DefaultDialogueKnot ?? string.Empty;

        /// <summary>
        /// Checks if this NPC can transition to an enemy.
        /// </summary>
        public bool CanBecomeEnemy =>
            Definition?.CanBecomeEnemy == true &&
            Definition.EnemyDefinition != null &&
            !HasTransitionedToEnemy;

        public override void Initialize(IPlatform platform)
        {
            if (Definition == null)
            {
                Debug.LogWarning($"[NpcContent] No definition assigned for NPC on platform {platform.Id}");
                return;
            }

            // Bind runtime instance from NpcPool if available
            TryBindRuntimeInstance();

            // NEW: Use NpcStoryProvider to dynamically select story if available
            if (_npcStoryProvider != null && _storyStateProvider != null)
            {
                SelectStoryDynamically();
            }
            else if (DialogueSession != null)
            {
                // Legacy: Use dialogue session if provided
                DialogueKnot = DialogueSession.FullInkPath;
            }
            else
            {
                // Fallback: Use default dialogue knot
                DialogueKnot = Definition.DefaultDialogueKnot;
            }

            SpawnNpcVisual(platform);
        }

        /// <summary>
        /// Attempts to bind the RuntimeInstance from the NpcPool.
        /// </summary>
        private void TryBindRuntimeInstance()
        {
            if (RuntimeInstance != null)
            {
                // Already bound
                return;
            }

            if (_npcPool == null || Definition == null)
            {
                return;
            }

            var npcId = Definition.NpcId;
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }

            var instance = _npcPool.GetNpc(npcId);
            if (instance != null)
            {
                RuntimeInstance = instance;
                Debug.Log($"[NpcContent] Auto-bound RuntimeInstance for NPC '{npcId}' from pool");
            }
        }

        /// <summary>
        /// Selects the appropriate story for this NPC encounter using the story provider.
        /// </summary>
        private void SelectStoryDynamically()
        {
            var currentState = _storyStateProvider.CurrentState;
            if (currentState == null)
            {
                Debug.LogWarning($"[NpcContent] No current state available for NPC '{Definition.NpcId}'");
                return;
            }

            // Get the next story for this NPC
            SelectedStory = _npcStoryProvider.GetNextStoryForNpc(Definition.NpcId, currentState);

            if (SelectedStory != null)
            {
                DialogueKnot = SelectedStory.StartingKnot;
                Debug.Log($"[NpcContent] Selected story '{SelectedStory.DisplayName}' for NPC '{Definition.DisplayName}'");
            }
            else
            {
                // No story available - use default dialogue
                DialogueKnot = Definition.DefaultDialogueKnot;
                Debug.Log($"[NpcContent] No story available for NPC '{Definition.DisplayName}', using default dialogue");
            }

            // Record this encounter for trigger condition tracking
            if (_npcStoryProvider is NpcStoryProvider provider)
            {
                provider.RecordNpcEncounter(Definition.NpcId);
            }
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
        /// Returns the created EnemyContent, or null if transition not possible.
        /// </summary>
        public EnemyContent TransitionToEnemy()
        {
            if (!CanBecomeEnemy)
            {
                Debug.LogWarning($"[NpcContent] Cannot transition NPC '{Definition?.NpcId}' to enemy");
                return null;
            }

            HasTransitionedToEnemy = true;

            // Create enemy content from the NPC's enemy definition
            var enemyContent = new EnemyContent
            {
                EnemyId = Definition.EnemyDefinition.EnemyId
            };

            Debug.Log($"[NpcContent] NPC '{Definition.DisplayName}' transitioned to enemy");
            OnTransitionedToEnemy?.Invoke(this, enemyContent);

            return enemyContent;
        }

        /// <summary>
        /// Notifies that dialogue has ended with a specific outcome.
        /// </summary>
        public void NotifyDialogueEnded()
        {
            OnDialogueEnded?.Invoke(this);
        }

        /// <summary>
        /// Binds a runtime NpcInstance to this content.
        /// Called by NpcContentInstanceBinder during platform initialization.
        /// </summary>
        /// <param name="instance">The NpcInstance to bind</param>
        public void BindRuntimeInstance(NpcInstance instance)
        {
            RuntimeInstance = instance;

            if (instance != null)
            {
                UnityEngine.Debug.Log($"[NpcContent] Bound RuntimeInstance '{instance.InstanceId}' to NPC '{Definition?.NpcId}'");
            }
        }

        /// <summary>
        /// Gets the effective NpcInstance, preferring RuntimeInstance if available.
        /// </summary>
        public NpcInstance GetEffectiveInstance()
        {
            return RuntimeInstance;
        }

        private void SpawnNpcVisual(IPlatform platform)
        {
            if (Definition?.Prefab == null)
            {
                Debug.LogWarning($"[NpcContent] No prefab assigned for NPC '{Definition?.NpcId}'");
                return;
            }

            var spawnPosition = platform.Visual?.Position ?? Vector3.zero;
            NpcInstance = UnityEngine.Object.Instantiate(Definition.Prefab, spawnPosition, Quaternion.identity);
            NpcInstance.name = $"NPC_{Definition.NpcId}";
        }

        /// <summary>
        /// Destroys the NPC visual instance.
        /// </summary>
        public void DestroyNpcVisual()
        {
            if (NpcInstance != null)
            {
                UnityEngine.Object.Destroy(NpcInstance);
                NpcInstance = null;
            }
        }
    }
}

