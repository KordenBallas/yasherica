using System.Linq;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Active state for cutscene playback. Drives the <see cref="CutsceneContent"/> lifecycle and
    /// completes the platform when the cutscene ends. (The legacy dialogue-presenter text display was
    /// removed with the legacy narrative engine; cutscenes are not emitted by the streaming planner.)
    /// </summary>
    public class CutsceneActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<CutsceneActiveState> { }

        private readonly IPlatformStateFactory _stateFactory;

        private IPlatform _currentPlatform;
        private CutsceneContent _cutsceneContent;

        [Inject]
        public CutsceneActiveState(IPlatformStateFactory stateFactory)
        {
            _stateFactory = stateFactory;
        }

        public override void OnEnter(IPlatform platform)
        {
            _currentPlatform = platform;

            _cutsceneContent = platform.Contents.OfType<CutsceneContent>().FirstOrDefault();
            if (_cutsceneContent == null)
            {
                Debug.LogWarning($"[CutsceneActiveState] No cutscene content found for platform {platform.Id}");
                TransitionToCompleted();
                return;
            }

            _cutsceneContent.OnCutsceneEnded += HandleCutsceneContentEnded;

            if (_cutsceneContent.HasCutscene)
            {
                _cutsceneContent.StartCutscene();
            }
            else
            {
                TransitionToCompleted();
            }
        }

        public override void OnExit(IPlatform platform)
        {
            if (_cutsceneContent != null)
            {
                _cutsceneContent.OnCutsceneEnded -= HandleCutsceneContentEnded;
            }

            _currentPlatform = null;
            _cutsceneContent = null;
        }

        private void HandleCutsceneContentEnded(CutsceneContent content, bool wasSkipped)
        {
            Debug.Log($"[CutsceneActiveState] Cutscene '{content.CutsceneId}' ended (skipped: {wasSkipped})");
            TransitionToCompleted();
        }

        private void TransitionToCompleted()
        {
            if (_currentPlatform == null)
            {
                return;
            }

            var completedState = _stateFactory.CreateCompletedState();
            _currentPlatform.TransitionToState(completedState);
        }
    }
}
