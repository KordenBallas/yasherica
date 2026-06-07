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
        private readonly IInkExternalFunctionBinder _externalFunctionBinder;

        private IPlatform _currentPlatform;
        private NpcContent _npcContent;
        private DialogueContent _dialogueContent;
        private bool _combatTriggered;

        [Inject]
        public DialogueActiveState(
            IDialoguePresenter dialoguePresenter,
            IPlatformStateFactory stateFactory,
            IInkExternalFunctionBinder externalFunctionBinder = null)
        {
            _dialoguePresenter = dialoguePresenter;
            _stateFactory = stateFactory;
            _externalFunctionBinder = externalFunctionBinder;
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
                _externalFunctionBinder?.BindAllExternalFunctions();
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
                TransitionToCombat();
                return;
            }

            TransitionToCompleted();
        }

        private void TransitionToCombat()
        {
            if (_currentPlatform == null) return;
            var combatState = _stateFactory.CreateActiveState(_currentPlatform, ContentType.Enemy);
            _currentPlatform.TransitionToState(combatState);
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null) return;
            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }
    }
}
