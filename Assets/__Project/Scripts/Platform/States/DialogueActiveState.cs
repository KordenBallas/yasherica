using System;
using System.Linq;
using Narrative;
using Narrative.Dialogue;
using Narrative.Discovery;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for dialogue interactions.
    /// Integrates with Ink story system via DialoguePresenter.
    /// </summary>
    public class DialogueActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<DialogueActiveState> { }

        private readonly DialoguePresenter _dialoguePresenter;
        private readonly IStoryStateProvider _storyStateProvider;
        private readonly IPlatformStateFactory _stateFactory;
        private readonly ISideStoryProvider _sideStoryProvider;
        private readonly IStoryManager _storyManager;

        private IPlatform _currentPlatform;
        private NpcContent _npcContent;
        private DialogueContent _dialogueContent;
        private bool _combatTriggered;
        private string _triggeredEnemyId;
        private string _activeSideStoryId;

        [Inject]
        public DialogueActiveState(
            DialoguePresenter dialoguePresenter,
            IStoryStateProvider storyStateProvider,
            IPlatformStateFactory stateFactory,
            ISideStoryProvider sideStoryProvider,
            IStoryManager storyManager)
        {
            _dialoguePresenter = dialoguePresenter;
            _storyStateProvider = storyStateProvider;
            _stateFactory = stateFactory;
            _sideStoryProvider = sideStoryProvider;
            _storyManager = storyManager;
        }

        public override void OnEnter(IPlatform platform)
        {
            _currentPlatform = platform;
            _combatTriggered = false;
            _triggeredEnemyId = null;
            _activeSideStoryId = null;

            Debug.Log($"[DialogueActiveState] Entering dialogue for platform {platform.Id}");

            // Subscribe to dialogue events
            _dialoguePresenter.OnDialogueEnded += HandleDialogueEnded;
            _dialoguePresenter.OnCombatTriggered += HandleCombatTriggered;
            _dialoguePresenter.OnQuestTriggered += HandleQuestTriggered;

            // Check if this is a side story platform
            var storyData = platform.StoryData;
            if (storyData != null && storyData.IsSideStory)
            {
                StartSideStoryDialogue(storyData);
                return;
            }

            // Find dialogue content
            _npcContent = platform.Contents.OfType<NpcContent>().FirstOrDefault();
            _dialogueContent = platform.Contents.OfType<DialogueContent>().FirstOrDefault();

            if (_npcContent != null)
            {
                StartNpcDialogue();
            }
            else if (_dialogueContent != null)
            {
                StartPureDialogue();
            }
            else
            {
                Debug.LogWarning($"[DialogueActiveState] No dialogue content found for platform {platform.Id}");
                TransitionToCompleted();
            }
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[DialogueActiveState] Exiting dialogue for platform {platform.Id}");

            // Unsubscribe from events
            _dialoguePresenter.OnDialogueEnded -= HandleDialogueEnded;
            _dialoguePresenter.OnCombatTriggered -= HandleCombatTriggered;
            _dialoguePresenter.OnQuestTriggered -= HandleQuestTriggered;

            // Notify content that dialogue ended
            _npcContent?.NotifyDialogueEnded();
            _dialogueContent?.NotifyDialogueCompleted();

            // Record side story completion for cooldowns
            if (!string.IsNullOrEmpty(_activeSideStoryId))
            {
                _sideStoryProvider?.RecordSideStoryPlayed(_activeSideStoryId);
            }

            _currentPlatform = null;
            _npcContent = null;
            _dialogueContent = null;
            _activeSideStoryId = null;
        }

        private void StartNpcDialogue()
        {
            if (_npcContent == null)
                return;

            var npcId = _npcContent.Definition?.NpcId;
            var dialogueKnot = _npcContent.EffectiveDialogueKnot;

            if (string.IsNullOrEmpty(dialogueKnot))
            {
                Debug.LogWarning($"[DialogueActiveState] NPC '{npcId}' has no dialogue knot");
                TransitionToCompleted();
                return;
            }

            // Record NPC encounter in story state
            if (!string.IsNullOrEmpty(npcId))
            {
                _storyStateProvider?.RecordNpcEncounter(npcId);
            }

            _dialoguePresenter.StartNpcDialogue(npcId, dialogueKnot);
        }

        private void StartPureDialogue()
        {
            if (_dialogueContent == null)
                return;

            var dialogueKnot = _dialogueContent.DialogueKnot;

            if (string.IsNullOrEmpty(dialogueKnot))
            {
                Debug.LogWarning("[DialogueActiveState] DialogueContent has no dialogue knot");
                TransitionToCompleted();
                return;
            }

            _dialoguePresenter.StartDialogue(dialogueKnot, _dialogueContent.SpeakerName);
        }

        private void StartSideStoryDialogue(LevelGeneration.StoryPlatformData storyData)
        {
            if (_sideStoryProvider == null)
            {
                Debug.LogWarning("[DialogueActiveState] No side story provider available");
                TransitionToCompleted();
                return;
            }

            var sideStory = _sideStoryProvider.GetSideStoryById(storyData.SideStoryId);
            if (sideStory == null)
            {
                Debug.LogWarning($"[DialogueActiveState] Side story '{storyData.SideStoryId}' not found");
                TransitionToCompleted();
                return;
            }

            if (!sideStory.HasInkContent)
            {
                Debug.LogWarning($"[DialogueActiveState] Side story '{storyData.SideStoryId}' has no Ink content");
                TransitionToCompleted();
                return;
            }

            _activeSideStoryId = storyData.SideStoryId;

            Debug.Log($"[DialogueActiveState] Starting side story: {sideStory.DisplayName}");

            // Load the side story's Ink content
            _storyManager.LoadStory(sideStory.GetInkJson());

            // Record NPC encounter if applicable
            if (!string.IsNullOrEmpty(storyData.NpcId))
            {
                _storyStateProvider?.RecordNpcEncounter(storyData.NpcId);
            }

            // Start dialogue at the configured starting knot
            _dialoguePresenter.StartDialogue(sideStory.StartingKnot, null);
        }

        private void HandleDialogueEnded(DialogueOutcomeType outcome)
        {
            Debug.Log($"[DialogueActiveState] Dialogue ended with outcome: {outcome}");

            switch (outcome)
            {
                case DialogueOutcomeType.Combat:
                    HandleCombatOutcome();
                    break;

                case DialogueOutcomeType.Quest:
                    // Quest already triggered via OnQuestTriggered
                    TransitionToCompleted();
                    break;

                case DialogueOutcomeType.Trade:
                    // Trade system extension point
                    TransitionToCompleted();
                    break;

                case DialogueOutcomeType.Continue:
                case DialogueOutcomeType.Exit:
                default:
                    TransitionToCompleted();
                    break;
            }
        }

        private void HandleCombatTriggered(string enemyId)
        {
            _combatTriggered = true;
            _triggeredEnemyId = enemyId;
        }

        private void HandleQuestTriggered(string questId)
        {
            if (!string.IsNullOrEmpty(questId))
            {
                _storyStateProvider?.StartQuest(questId);
                Debug.Log($"[DialogueActiveState] Quest started: {questId}");
            }
        }

        private void HandleCombatOutcome()
        {
            if (_npcContent != null && _npcContent.CanBecomeEnemy)
            {
                // Transition NPC to enemy
                var enemyContent = _npcContent.TransitionToEnemy();

                if (enemyContent != null)
                {
                    // Add enemy content to platform
                    _currentPlatform.AddContent(enemyContent);

                    // Destroy NPC visual
                    _npcContent.DestroyNpcVisual();

                    // Transition to combat
                    TransitionToCombat();
                    return;
                }
            }

            // If no enemy transition possible, just complete
            TransitionToCompleted();
        }

        private void TransitionToCombat()
        {
            if (_currentPlatform == null)
                return;

            var combatState = _stateFactory.CreateActiveState(_currentPlatform);
            _currentPlatform.TransitionToState(combatState);
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null)
                return;

            // Mark story node as completed if applicable
            CompleteStoryNode();

            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }

        private void CompleteStoryNode()
        {
            if (_storyStateProvider == null)
                return;

            // Try to find the story node ID for this platform
            string nodeId = null;

            // Check if this is a side story completion
            if (!string.IsNullOrEmpty(_activeSideStoryId))
            {
                nodeId = $"sidestory_{_activeSideStoryId}";
            }
            else if (_npcContent?.Definition != null)
            {
                nodeId = $"npc_{_npcContent.Definition.NpcId}";
            }
            else if (_dialogueContent != null)
            {
                nodeId = _dialogueContent.DialogueKnot;
            }

            if (!string.IsNullOrEmpty(nodeId))
            {
                _storyStateProvider.CompleteStoryNode(nodeId);
            }
        }
    }
}
