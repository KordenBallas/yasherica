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

        private readonly IDialoguePresenter _dialoguePresenter;
        private readonly IPlatformStateFactory _stateFactory;
        private readonly ISideStoryProvider _sideStoryProvider;
        private readonly IStoryManager _storyManager;
        private readonly IDialogueSessionInitializer _sessionInitializer;
        private readonly IDialogueOutcomeHandler _outcomeHandler;
        private readonly ICombatTransitionHandler _combatHandler;

        private IPlatform _currentPlatform;
        private NpcContent _npcContent;
        private DialogueContent _dialogueContent;
        private IDialogueContext _currentContext;
        private bool _combatTriggered;
        private string _triggeredEnemyId;
        private string _activeSideStoryId;

        [Inject]
        public DialogueActiveState(
            IDialoguePresenter dialoguePresenter,
            IPlatformStateFactory stateFactory,
            ISideStoryProvider sideStoryProvider,
            IStoryManager storyManager,
            IDialogueSessionInitializer sessionInitializer = null,
            IDialogueOutcomeHandler outcomeHandler = null,
            ICombatTransitionHandler combatHandler = null)
        {
            _dialoguePresenter = dialoguePresenter;
            _stateFactory = stateFactory;
            _sideStoryProvider = sideStoryProvider;
            _storyManager = storyManager;
            _sessionInitializer = sessionInitializer;
            _outcomeHandler = outcomeHandler;
            _combatHandler = combatHandler;
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

            ResetDialogueState();
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

            // Create dialogue context with NPC runtime instance if available
            _currentContext = DialogueContext.ForNpc(
                npcId,
                dialogueKnot,
                _npcContent.Definition?.DisplayName,
                _npcContent.RuntimeInstance);

            // Initialize session (ensures external functions are bound)
            _sessionInitializer?.InitializeSession(_currentContext);

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

            // Create dialogue context for pure dialogue
            _currentContext = DialogueContext.ForDialogue(dialogueKnot, _dialogueContent.SpeakerName);

            // Initialize session (ensures external functions are bound)
            _sessionInitializer?.InitializeSession(_currentContext);

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

            // Create dialogue context for side story
            _currentContext = DialogueContext.ForSideStory(storyData.SideStoryId, sideStory, storyData.NpcId);

            // Load the side story's Ink content
            _storyManager.LoadStory(sideStory.GetInkJson());

            // Initialize session (binds external functions)
            _sessionInitializer?.EnsureExternalFunctionsBound();

            // Start dialogue at the configured starting knot
            _dialoguePresenter.StartDialogue(sideStory.StartingKnot, null);
        }

        private void HandleDialogueEnded(DialogueOutcomeType outcome)
        {
            Debug.Log($"[DialogueActiveState] Dialogue ended with outcome: {outcome}");

            // Delegate outcome handling to the outcome handler
            _outcomeHandler?.HandleOutcome(outcome, _currentContext);

            switch (outcome)
            {
                case DialogueOutcomeType.Combat:
                    HandleCombatOutcome();
                    break;

                case DialogueOutcomeType.Quest:
                case DialogueOutcomeType.Trade:
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
                _outcomeHandler?.StartQuest(questId);
                Debug.Log($"[DialogueActiveState] Quest triggered: {questId}");
            }
        }

        private void HandleCombatOutcome()
        {
            // Use combat handler if available
            if (_combatHandler != null)
            {
                if (_combatHandler.CanTransitionToCombat(_currentPlatform, _currentContext, _combatTriggered))
                {
                    // Prepare enemy content if transitioning from NPC
                    EnemyContent enemyContent = null;
                    if (_npcContent != null && _npcContent.CanBecomeEnemy)
                    {
                        enemyContent = _combatHandler.PrepareEnemyContent(_npcContent);
                    }

                    _combatHandler.ExecuteTransition(_currentPlatform, _npcContent, enemyContent);
                    return;
                }
            }
            else
            {
                // Fallback: original implementation
                if (_npcContent != null && _npcContent.CanBecomeEnemy)
                {
                    var enemyContent = _npcContent.TransitionToEnemy();

                    if (enemyContent != null)
                    {
                        _currentPlatform.AddContent(enemyContent);
                        _npcContent.DestroyNpcVisual();
                        TransitionToCombat();
                        return;
                    }
                }

                if (_combatTriggered)
                {
                    Debug.Log("[DialogueActiveState] Combat triggered via Ink function - transitioning to combat");
                    TransitionToCombat();
                    return;
                }
            }

            // If no combat transition possible, complete normally
            TransitionToCompleted();
        }

        private void TransitionToCombat()
        {
            if (_currentPlatform == null)
                return;

            var combatState = _stateFactory.CreateActiveState(_currentPlatform, ContentType.Enemy);
            _currentPlatform.TransitionToState(combatState);
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null)
                return;

            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }

        private void ResetDialogueState()
        {
            _currentPlatform = null;
            _npcContent = null;
            _dialogueContent = null;
            _currentContext = null;
            _activeSideStoryId = null;
        }
    }
}
