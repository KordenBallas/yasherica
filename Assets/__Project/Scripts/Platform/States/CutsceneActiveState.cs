using System;
using System.Linq;
using Narrative;
using Narrative.Dialogue;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for cutscene playback.
    /// Uses Ink story system for cutscene content with dialogue presenter.
    /// </summary>
    public class CutsceneActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<CutsceneActiveState> { }

        private readonly IDialoguePresenter _dialoguePresenter;
        private readonly IPlatformStateFactory _stateFactory;

        private IPlatform _currentPlatform;
        private CutsceneContent _cutsceneContent;

        [Inject]
        public CutsceneActiveState(
            IDialoguePresenter dialoguePresenter,
            IPlatformStateFactory stateFactory)
        {
            _dialoguePresenter = dialoguePresenter;
            _stateFactory = stateFactory;
        }

        public override void OnEnter(IPlatform platform)
        {
            _currentPlatform = platform;

            Debug.Log($"[CutsceneActiveState] Entering cutscene for platform {platform.Id}");

            _cutsceneContent = platform.Contents.OfType<CutsceneContent>().FirstOrDefault();

            if (_cutsceneContent == null)
            {
                Debug.LogWarning($"[CutsceneActiveState] No cutscene content found for platform {platform.Id}");
                TransitionToCompleted();
                return;
            }

            // Subscribe to dialogue events (cutscenes use dialogue for text display)
            _dialoguePresenter.OnDialogueEnded += HandleCutsceneEnded;

            // Subscribe to cutscene events
            _cutsceneContent.OnCutsceneEnded += HandleCutsceneContentEnded;

            // Start the cutscene
            if (_cutsceneContent.HasCutscene)
            {
                _cutsceneContent.StartCutscene();
                _dialoguePresenter.StartDialogue(_cutsceneContent.CutsceneKnot, "Narrator");
            }
            else
            {
                TransitionToCompleted();
            }
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[CutsceneActiveState] Exiting cutscene for platform {platform.Id}");

            _dialoguePresenter.OnDialogueEnded -= HandleCutsceneEnded;

            if (_cutsceneContent != null)
            {
                _cutsceneContent.OnCutsceneEnded -= HandleCutsceneContentEnded;
            }

            _currentPlatform = null;
            _cutsceneContent = null;
        }

        private void HandleCutsceneEnded(DialogueOutcomeType outcome)
        {
            _cutsceneContent?.CompleteCutscene();
        }

        private void HandleCutsceneContentEnded(CutsceneContent content, bool wasSkipped)
        {
            Debug.Log($"[CutsceneActiveState] Cutscene '{content.CutsceneId}' ended (skipped: {wasSkipped})");
            TransitionToCompleted();
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null)
                return;

            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }
    }
}
