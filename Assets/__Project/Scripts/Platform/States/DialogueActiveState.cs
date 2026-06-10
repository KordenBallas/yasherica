using System;
using System.Linq;
using Narrative;
using Narrative.Dialogue;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for dialogue interactions.
    /// Uses NpcAssignment from NpcContent for composite dialogue.
    /// Handles outcomes including combat transitions.
    /// </summary>
    public class DialogueActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<DialogueActiveState> { }

        private readonly IDialoguePresenter _dialoguePresenter;
        private readonly IPlatformStateFactory _stateFactory;

        [Inject(Id = "story")]
        private IStoryManager _storyManager;

        [Inject(Id = "npc")]
        private IStoryManager _npcManager;

        private IPlatform _currentPlatform;
        private NpcContent _npcContent;
        private DialogueContent _dialogueContent;
        private bool _combatTriggered;

        [Inject]
        public DialogueActiveState(
            IDialoguePresenter dialoguePresenter,
            IPlatformStateFactory stateFactory)
        {
            _dialoguePresenter = dialoguePresenter;
            _stateFactory = stateFactory;
        }

        public override void OnEnter(IPlatform platform)
        {
            _currentPlatform = platform;
            _combatTriggered = false;

            _dialoguePresenter.OnDialogueEnded += HandleDialogueEnded;
            _dialoguePresenter.OnCombatTriggered += HandleCombatTriggered;
            _dialoguePresenter.OnQuestTriggered += HandleQuestTriggered;

            _npcContent = platform.Contents.OfType<NpcContent>().FirstOrDefault();
            _dialogueContent = platform.Contents.OfType<DialogueContent>().FirstOrDefault();

            if (_npcContent?.Assignment != null)
            {
                _dialoguePresenter.StartDialogue(_npcContent.Assignment);
            }
            else if (_dialogueContent != null)
            {
                StartPureDialogue();
            }
            else
            {
                Debug.LogWarning($"[DialogueActiveState] No dialogue content on platform {platform.Id}");
                TransitionToCompleted();
            }
        }

        public override void OnExit(IPlatform platform)
        {
            _dialoguePresenter.OnDialogueEnded -= HandleDialogueEnded;
            _dialoguePresenter.OnCombatTriggered -= HandleCombatTriggered;
            _dialoguePresenter.OnQuestTriggered -= HandleQuestTriggered;

            // Reset story managers to prevent state pollution between dialogues
            _storyManager?.Reset();
            _npcManager?.Reset();

            // Reset NPC combat state to allow re-triggering combat
            _npcContent?.ResetCombatState();

            _npcContent?.NotifyDialogueEnded();
            _dialogueContent?.NotifyDialogueCompleted();

            _currentPlatform = null;
            _npcContent = null;
            _dialogueContent = null;
        }

        private void StartPureDialogue()
        {
            if (_dialogueContent == null || !_dialogueContent.HasDialogue)
            {
                TransitionToCompleted();
                return;
            }

            _dialoguePresenter.StartDialogue(
                _dialogueContent.DialogueKnot,
                _dialogueContent.SpeakerName);
        }

        private void HandleDialogueEnded(DialogueOutcomeType outcome)
        {
            switch (outcome)
            {
                case DialogueOutcomeType.Combat:
                    HandleCombatOutcome();
                    break;
                default:
                    TransitionToCompleted();
                    break;
            }
        }

        private void HandleCombatTriggered(string enemyId)
        {
            _combatTriggered = true;
        }

        private void HandleQuestTriggered(string questId)
        {
            if (!string.IsNullOrEmpty(questId))
                Debug.Log($"[DialogueActiveState] Quest triggered: {questId}");
        }

        private void HandleCombatOutcome()
        {
            Debug.Log($"[DialogueActiveState] HandleCombatOutcome: _npcContent={_npcContent != null}, CanBecomeEnemy={_npcContent?.CanBecomeEnemy}, _combatTriggered={_combatTriggered}");

            // Path 1: NPC can transition to enemy
            if (_npcContent != null && _npcContent.CanBecomeEnemy)
            {
                var enemyContent = _npcContent.TransitionToEnemy();
                if (enemyContent != null)
                {
                    _currentPlatform.AddContent(enemyContent);
                    _npcContent.DestroyNpcVisual();
                    Debug.Log("[DialogueActiveState] Path 1: NPC transitioned to enemy, starting combat");
                    TransitionToCombat();
                    return;
                }
            }

            // Path 2: Combat was explicitly triggered via Ink function
            if (_combatTriggered)
            {
                Debug.Log("[DialogueActiveState] Path 2: Combat triggered via Ink function");
                TransitionToCombat();
                return;
            }

            // Path 3: Fallback
            Debug.LogWarning("[DialogueActiveState] Path 3: Combat outcome signaled but no enemy transition. Starting combat anyway.");
            TransitionToCombat();
        }

        private void TransitionToCombat()
        {
            if (_currentPlatform == null)
            {
                Debug.LogError("[DialogueActiveState] Cannot transition to combat: platform is null");
                return;
            }

            try
            {
                var combatState = _stateFactory.CreateActiveState(_currentPlatform, ContentType.Enemy);
                _currentPlatform.TransitionToState(combatState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueActiveState] Failed to transition to combat: {ex.Message}\n{ex.StackTrace}");
                TransitionToCompleted();
            }
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null) return;
            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }
    }
}
