using System;
using System.Collections.Generic;
using Core.Logging;
using Zenject;

namespace Narrative.Dialogue
{
    /// <summary>
    /// MVP presenter bridging the pure-C# <see cref="DialogueRunner"/> to the existing
    /// <see cref="IDialogueView"/>. The runner is deliberately view-agnostic (so it stays unit-testable);
    /// this thin adapter turns its events into view calls and forwards the view's input back into the
    /// runner — the same role <c>CompositeDialoguePresenter</c> plays for the legacy path, but for the
    /// new fact-driven runner and reusing the same view.
    ///
    /// Combat/quest signals are re-exposed as passthrough events for the later encounter integration.
    ///
    /// Continue-gated flow: the runner emits one readable line then parks in
    /// <see cref="DialogueRunnerState.AwaitingContinue"/>, so a multi-line knot is read one line at a
    /// time. The view's <see cref="IDialogueView.OnContinueClicked"/> /
    /// <see cref="IDialogueView.OnSkipRequested"/> advance past the gate via
    /// <see cref="DialogueRunner.Continue"/>.
    /// </summary>
    public sealed class DialogueRunnerViewPresenter : IInitializable, IDisposable
    {
        private readonly DialogueRunner _runner;
        private readonly IDialogueView _view;
        private readonly IGameLogger _logger;

        /// <summary>Raised when the runner requests combat (enemy id); for the encounter integration.</summary>
        public event Action<string> OnCombatTriggered;

        /// <summary>Raised when the runner starts a quest (quest id); for the encounter integration.</summary>
        public event Action<string> OnQuestStarted;

        public DialogueRunnerViewPresenter(DialogueRunner runner, IDialogueView view, IGameLogger logger)
        {
            _runner = runner;
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            _runner.OnSpeakerChanged += HandleSpeakerChanged;
            _runner.OnLine += HandleLine;
            _runner.OnChoices += HandleChoices;
            _runner.OnDialogueEnded += HandleDialogueEnded;
            _runner.OnCombatTriggered += HandleCombatTriggered;
            _runner.OnQuestStarted += HandleQuestStarted;

            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.OnContinueClicked += HandleContinue;
            _view.OnSkipRequested += HandleContinue;
        }

        public void Dispose()
        {
            _runner.OnSpeakerChanged -= HandleSpeakerChanged;
            _runner.OnLine -= HandleLine;
            _runner.OnChoices -= HandleChoices;
            _runner.OnDialogueEnded -= HandleDialogueEnded;
            _runner.OnCombatTriggered -= HandleCombatTriggered;
            _runner.OnQuestStarted -= HandleQuestStarted;

            _view.OnChoiceSelected -= HandleChoiceSelected;
            _view.OnContinueClicked -= HandleContinue;
            _view.OnSkipRequested -= HandleContinue;
        }

        private void HandleSpeakerChanged(string speaker) => _view.SetSpeakerName(speaker);

        private void HandleLine(string text)
        {
            _view.Show();
            _view.HideChoices();
            _view.SetDialogueText(text);
            _view.ShowContinueButton(); // every line gates on continue (see DialogueRunner.Continue)
        }

        private void HandleChoices(IReadOnlyList<StoryChoice> choices)
        {
            _view.HideContinueButton();
            _view.ShowChoices(ToDialogueChoices(choices));
        }

        private void HandleDialogueEnded(string outcome)
        {
            _view.HideChoices();
            _view.Hide();
        }

        private void HandleCombatTriggered(string enemyId)
        {
            // Combat runs outside Ink; the view parks until the encounter reports a result.
            _view.HideChoices();
            OnCombatTriggered?.Invoke(enemyId);
        }

        private void HandleQuestStarted(string questId) => OnQuestStarted?.Invoke(questId);

        private void HandleChoiceSelected(int index) => _runner.SelectChoice(index);

        // Continue and skip both advance past the gated line; a whole-dialogue abort is out of scope.
        private void HandleContinue() => _runner.Continue();

        private static IReadOnlyList<DialogueChoice> ToDialogueChoices(IReadOnlyList<StoryChoice> choices)
        {
            var result = new List<DialogueChoice>(choices?.Count ?? 0);
            if (choices == null)
            {
                return result;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                result.Add(new DialogueChoice(choice.Index, choice.Text, true, choice.Tags));
            }

            return result;
        }
    }
}
