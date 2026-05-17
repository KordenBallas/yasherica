using System;
using Narrative.Generation;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Default implementation of IDialogueOutcomeHandler.
    /// Handles quest starts, completions, relationship updates, and story recording.
    /// </summary>
    public class DialogueOutcomeHandler : IDialogueOutcomeHandler
    {
        private readonly IStoryStateProvider _storyStateProvider;
        private readonly IQuestManager _questManager;
        private readonly INpcPool _npcPool;

        public event Action<string> OnQuestStart;
        public event Action<string, DialogueOutcomeType> OnQuestCompleted;
        public event Action<string, int> OnRelationshipChanged;

        public DialogueOutcomeHandler(
            IStoryStateProvider storyStateProvider,
            IQuestManager questManager = null,
            INpcPool npcPool = null)
        {
            _storyStateProvider = storyStateProvider;
            _questManager = questManager;
            _npcPool = npcPool;
        }

        public void HandleOutcome(DialogueOutcomeType outcome, IDialogueContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("[DialogueOutcomeHandler] Cannot handle outcome: context is null");
                return;
            }

            Debug.Log($"[DialogueOutcomeHandler] Handling outcome {outcome} for NPC '{context.NpcId}'");

            // Notify quest manager of dialogue completion
            if (context.BoundStory != null)
            {
                _questManager?.OnDialogueCompleted(context.BoundStory, outcome);
            }

            switch (outcome)
            {
                case DialogueOutcomeType.Quest:
                    HandleQuestOutcome(context);
                    break;

                case DialogueOutcomeType.Continue:
                case DialogueOutcomeType.Exit:
                    HandleStandardCompletion(context);
                    break;

                case DialogueOutcomeType.Combat:
                    // Combat outcome is handled by ICombatTransitionHandler
                    HandleStandardCompletion(context);
                    break;

                case DialogueOutcomeType.Trade:
                    // Trade is an extension point
                    HandleStandardCompletion(context);
                    break;
            }
        }

        public void StartQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return;

            _storyStateProvider?.StartQuest(questId);
            OnQuestStart?.Invoke(questId);
            Debug.Log($"[DialogueOutcomeHandler] Quest started: {questId}");
        }

        public void CompleteStoryNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            _storyStateProvider?.CompleteStoryNode(nodeId);
            Debug.Log($"[DialogueOutcomeHandler] Story node completed: {nodeId}");
        }

        public void UpdateRelationship(string npcId, int delta)
        {
            if (string.IsNullOrEmpty(npcId))
                return;

            // Update in NpcPool if available
            var npc = _npcPool?.GetNpc(npcId);
            if (npc != null)
            {
                npc.UpdateRelationship(delta);
                Debug.Log($"[DialogueOutcomeHandler] Updated relationship with '{npcId}': {delta:+#;-#;0} (now: {npc.RelationshipScore})");
            }

            OnRelationshipChanged?.Invoke(npcId, delta);
        }

        private void HandleQuestOutcome(IDialogueContext context)
        {
            // If this dialogue is part of a bound story with quest
            if (context.BoundStory != null)
            {
                // Quest completion is handled through quest manager events
                Debug.Log($"[DialogueOutcomeHandler] Quest outcome for bound story");
            }

            HandleStandardCompletion(context);
        }

        private void HandleStandardCompletion(IDialogueContext context)
        {
            // Complete story node based on context type
            string nodeId = DetermineNodeId(context);
            if (!string.IsNullOrEmpty(nodeId))
            {
                CompleteStoryNode(nodeId);
            }

            // Update NPC state if available
            if (context.BoundNpcInstance != null)
            {
                context.BoundNpcInstance.MarkEncountered();
            }
        }

        private string DetermineNodeId(IDialogueContext context)
        {
            if (context.IsSideStory && !string.IsNullOrEmpty(context.SideStoryId))
            {
                return $"sidestory_{context.SideStoryId}";
            }

            if (!string.IsNullOrEmpty(context.NpcId))
            {
                return $"npc_{context.NpcId}";
            }

            if (!string.IsNullOrEmpty(context.DialogueKnot))
            {
                return context.DialogueKnot;
            }

            return null;
        }
    }
}
