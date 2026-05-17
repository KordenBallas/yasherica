using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data.Definitions;
using Narrative.Data.Providers;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Coordinates between dialogue model, view, and Ink story manager.
    /// Pure C# class following MVP pattern.
    /// </summary>
    public class DialoguePresenter : IDialoguePresenter, IDisposable
    {
        private readonly DialogueModel _model;
        private readonly IStoryManager _storyManager;
        private readonly INpcDataProvider _npcDataProvider;

        private IDialogueView _view;
        private NpcDefinition _currentNpc;
        private bool _isTypewriting;
        private string _pendingText;

        /// <summary>
        /// Event fired when dialogue session ends.
        /// </summary>
        public event Action<DialogueOutcomeType> OnDialogueEnded;

        /// <summary>
        /// Event fired when a combat outcome is triggered.
        /// </summary>
        public event Action<string> OnCombatTriggered;

        /// <summary>
        /// Event fired when a quest outcome is triggered.
        /// </summary>
        public event Action<string> OnQuestTriggered;

        /// <summary>
        /// The current dialogue model.
        /// </summary>
        public DialogueModel Model => _model;

        /// <summary>
        /// Whether dialogue is currently active.
        /// </summary>
        public bool IsActive => _model.IsActive;

        public DialoguePresenter(
            IStoryManager storyManager,
            INpcDataProvider npcDataProvider,
            IDialogueView view)
        {
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _npcDataProvider = npcDataProvider;
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = new DialogueModel();

            SubscribeToStoryEvents();
            SubscribeToViewEvents();
        }

        /// <summary>
        /// Starts a dialogue with an NPC.
        /// </summary>
        public void StartNpcDialogue(string npcId, string dialogueKnot)
        {
            if (string.IsNullOrEmpty(dialogueKnot))
            {
                Debug.LogError("[DialoguePresenter] Cannot start dialogue: no knot specified");
                return;
            }

            _currentNpc = _npcDataProvider?.GetNpcById(npcId);
            var speakerName = _currentNpc?.DisplayName ?? "Unknown";

            _model.StartDialogue(npcId, speakerName);

            // Navigate to the dialogue knot
            _storyManager.GoToKnot(dialogueKnot);

            // Show the view and continue to first line
            _view?.Show();
            SetupNpcPortrait();
            ContinueDialogue();
        }

        /// <summary>
        /// Starts a dialogue from a specific knot (no NPC).
        /// </summary>
        public void StartDialogue(string dialogueKnot, string speakerName = null)
        {
            if (string.IsNullOrEmpty(dialogueKnot))
            {
                Debug.LogError("[DialoguePresenter] Cannot start dialogue: no knot specified");
                return;
            }

            _currentNpc = null;
            _model.StartDialogue(null, speakerName);

            _storyManager.GoToKnot(dialogueKnot);

            _view?.Show();
            _view?.ClearPortrait();
            ContinueDialogue();
        }

        /// <summary>
        /// Continues to the next line of dialogue.
        /// </summary>
        public void ContinueDialogue()
        {
            if (!_model.IsActive)
                return;

            // Skip typewriter if active
            if (_isTypewriting)
            {
                _view?.SkipTypewriterEffect();
                _isTypewriting = false;
                ShowDialogueText(_pendingText);
                return;
            }

            if (!_storyManager.CanContinue && !_storyManager.HasChoices)
            {
                EndDialogue(DialogueOutcomeType.Continue);
                return;
            }

            if (_storyManager.CanContinue)
            {
                var text = _storyManager.Continue();
                ProcessTags(_storyManager.CurrentTags);
                ShowDialogueText(text);
            }

            UpdateChoicesDisplay();
            UpdateContinueButton();
        }

        /// <summary>
        /// Selects a choice by index.
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            Debug.Log($"[DialoguePresenter] SelectChoice called: {choiceIndex}");
            Debug.Log($"  IsActive: {_model.IsActive}");
            Debug.Log($"  HasChoices: {_model.HasChoices}");
            Debug.Log($"  Choices count: {_model.Choices.Count}");

            if (!_model.IsActive || !_model.HasChoices)
            {
                Debug.LogWarning("[DialoguePresenter] Cannot select choice - dialogue not active or no choices");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= _model.Choices.Count)
            {
                Debug.LogError($"[DialoguePresenter] Invalid choice index: {choiceIndex}");
                return;
            }

            Debug.Log($"[DialoguePresenter] Choosing choice {choiceIndex} in story manager");
            _storyManager.ChooseChoice(choiceIndex);
            _view?.HideChoices();

            Debug.Log("[DialoguePresenter] Continuing dialogue after choice");
            ContinueDialogue();
        }

        /// <summary>
        /// Ends the dialogue session.
        /// </summary>
        public void EndDialogue(DialogueOutcomeType outcome)
        {
            _model.EndDialogue(outcome);
            _view?.Hide();
            _currentNpc = null;

            Debug.Log($"[DialoguePresenter] Dialogue ended with outcome: {outcome}");
            OnDialogueEnded?.Invoke(outcome);
        }

        /// <summary>
        /// Requests to skip/exit the dialogue.
        /// </summary>
        public void RequestSkip()
        {
            if (_isTypewriting)
            {
                _view?.SkipTypewriterEffect();
                _isTypewriting = false;
                ShowDialogueText(_pendingText);
            }
            else
            {
                EndDialogue(DialogueOutcomeType.Exit);
            }
        }

        /// <summary>
        /// Fires the combat triggered event. Used by external function bindings.
        /// </summary>
        public void FireCombatTriggered(string enemyId)
        {
            OnCombatTriggered?.Invoke(enemyId);
        }

        public void Dispose()
        {
            UnsubscribeFromStoryEvents();
            UnsubscribeFromViewEvents();
            _model.Clear();
        }

        private void ShowDialogueText(string text)
        {
            _model.SetText(text);
            _view?.SetDialogueText(text);
        }

        private void SetupNpcPortrait()
        {
            if (_currentNpc?.Portrait != null)
            {
                _view?.SetPortrait(_currentNpc.Portrait);
            }
            else
            {
                _view?.ClearPortrait();
            }
        }

        private void UpdateChoicesDisplay()
        {
            if (_storyManager.HasChoices)
            {
                var choices = _storyManager.CurrentChoices
                    .Select(c => new DialogueChoice(c.Index, c.Text, true, c.Tags))
                    .ToList();

                Debug.Log($"[DialoguePresenter] UpdateChoicesDisplay - {choices.Count} choices from story:");
                foreach (var choice in choices)
                {
                    Debug.Log($"  Choice {choice.Index}: '{choice.Text}' (Enabled: {choice.IsEnabled})");
                }

                _model.SetChoices(choices);
                _view?.ShowChoices(choices);
                _view?.HideContinueButton();
            }
            else
            {
                Debug.Log("[DialoguePresenter] UpdateChoicesDisplay - No choices available");
                _model.SetChoices(Array.Empty<DialogueChoice>());
                _view?.HideChoices();
            }
        }

        private void UpdateContinueButton()
        {
            _model.SetCanContinue(_storyManager.CanContinue);

            if (_storyManager.CanContinue && !_storyManager.HasChoices)
            {
                _view?.ShowContinueButton();
            }
            else if (!_storyManager.CanContinue && !_storyManager.HasChoices)
            {
                // End of dialogue - show continue to close
                _view?.ShowContinueButton();
            }
            else
            {
                _view?.HideContinueButton();
            }
        }

        private void ProcessTags(IReadOnlyList<string> tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                ProcessTag(tag);
            }
        }

        private void ProcessTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            var colonIndex = tag.IndexOf(':');
            if (colonIndex <= 0)
                return;

            var key = tag.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = tag.Substring(colonIndex + 1).Trim();

            switch (key)
            {
                case "speaker":
                    _model.SetSpeaker(value);
                    _view?.SetSpeakerName(value);
                    break;

                case "outcome":
                    HandleOutcomeTag(value);
                    break;

                case "quest":
                    OnQuestTriggered?.Invoke(value);
                    break;

                case "combat":
                    OnCombatTriggered?.Invoke(value);
                    break;
            }
        }

        private void HandleOutcomeTag(string outcome)
        {
            switch (outcome.ToLowerInvariant())
            {
                case "combat":
                    EndDialogue(DialogueOutcomeType.Combat);
                    break;

                case "quest":
                    EndDialogue(DialogueOutcomeType.Quest);
                    break;

                case "trade":
                    EndDialogue(DialogueOutcomeType.Trade);
                    break;

                case "exit":
                    EndDialogue(DialogueOutcomeType.Exit);
                    break;
            }
        }

        private void SubscribeToStoryEvents()
        {
            _storyManager.OnStoryEnded += HandleStoryEnded;
        }

        private void UnsubscribeFromStoryEvents()
        {
            _storyManager.OnStoryEnded -= HandleStoryEnded;
        }

        private void SubscribeToViewEvents()
        {
            if (_view == null)
            {
                Debug.LogError("[DialoguePresenter] Cannot subscribe to view events: view is null!");
                return;
            }

            _view.OnContinueClicked += HandleContinueClicked;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.OnSkipRequested += HandleSkipRequested;

            Debug.Log("[DialoguePresenter] Successfully subscribed to view events");
        }

        private void UnsubscribeFromViewEvents()
        {
            if (_view == null)
                return;

            _view.OnContinueClicked -= HandleContinueClicked;
            _view.OnChoiceSelected -= HandleChoiceSelected;
            _view.OnSkipRequested -= HandleSkipRequested;
        }

        private void HandleContinueClicked()
        {
            ContinueDialogue();
        }

        private void HandleChoiceSelected(int index)
        {
            Debug.Log($"[DialoguePresenter] HandleChoiceSelected called with index: {index}");
            SelectChoice(index);
        }

        private void HandleSkipRequested()
        {
            RequestSkip();
        }

        private void HandleStoryEnded()
        {
            if (!_model.IsActive)
                return;

            // Check if there are unprocessed outcome tags
            var currentTags = _storyManager.CurrentTags;
            if (currentTags != null && currentTags.Count > 0)
            {
                foreach (var tag in currentTags)
                {
                    var colonIndex = tag.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = tag.Substring(0, colonIndex).Trim().ToLowerInvariant();
                        if (key == "outcome")
                        {
                            var value = tag.Substring(colonIndex + 1).Trim();
                            HandleOutcomeTag(value);
                            return; // Exit early - outcome tag will call EndDialogue
                        }
                    }
                }
            }

            // No outcome tag found, use default Continue outcome
            EndDialogue(DialogueOutcomeType.Continue);
        }
    }
}
