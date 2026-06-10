using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using Narrative.Generation;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Runs two IStoryManager instances in parallel: one for the quest/encounter Ink
    /// and one for the NPC's character Ink. Combines choices from both into a unified list.
    /// </summary>
    public class CompositeDialoguePresenter : IDialoguePresenter, IDisposable
    {
        private readonly DialogueModel _model;
        private readonly IDialogueView _view;

        // Two story managers: one for quest, one for NPC character
        private readonly IStoryManager _storyManager;
        private readonly IStoryManager _npcManager;
        private readonly IInkExternalFunctionBinder _externalFunctionBinder;

        private NpcAssignment _currentAssignment;
        private bool _storyActive;
        private bool _npcActive;
        private bool _isTypewriting;
        private string _pendingText;
        private DialogueOutcomeType? _pendingOutcome;

        // Track which manager owns which choice indices
        private readonly List<ChoiceSource> _choiceSources = new();

        public event Action<DialogueOutcomeType> OnDialogueEnded;
        public event Action<string> OnCombatTriggered;
        public event Action<string> OnQuestTriggered;

        public DialogueModel Model => _model;
        public bool IsActive => _model.IsActive;

        public CompositeDialoguePresenter(
            IStoryManager storyManager,
            IStoryManager npcManager,
            IInkExternalFunctionBinder externalFunctionBinder,
            IDialogueView view)
        {
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _npcManager = npcManager ?? throw new ArgumentNullException(nameof(npcManager));
            _externalFunctionBinder = externalFunctionBinder ?? throw new ArgumentNullException(nameof(externalFunctionBinder));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = new DialogueModel();

            SubscribeToViewEvents();
            SubscribeToBinderEvents();
        }

        public void StartDialogue(NpcAssignment assignment)
        {
            if (assignment == null)
            {
                Debug.LogError("[CompositeDialoguePresenter] Cannot start dialogue: assignment is null");
                return;
            }

            _currentAssignment = assignment;
            var npcName = assignment.Npc.DisplayName;

            _model.StartDialogue(assignment.Npc.NpcId, npcName);

            // Load story Ink if present
            _storyActive = false;
            if (assignment.HasStory && assignment.Story.HasInkContent)
            {
                _storyManager.LoadStory(assignment.Story.GetInkJson());
                _storyManager.GoToKnot(assignment.Story.StartingKnot);
                _storyManager.SetVariable("npc_name", npcName);
                _externalFunctionBinder.BindToStoryManager();
                _storyActive = true;
            }

            // Load NPC character Ink if present
            _npcActive = false;
            if (assignment.Npc.HasCharacterInk)
            {
                _npcManager.LoadStory(assignment.Npc.GetCharacterInkJson());
                _npcManager.GoToKnot(assignment.Npc.CharacterStartKnot);
                _externalFunctionBinder.BindToNpcManager();
                _npcActive = true;
            }

            if (!_storyActive && !_npcActive)
            {
                Debug.LogWarning($"[CompositeDialoguePresenter] No Ink content for NPC '{npcName}'");
                EndDialogue(DialogueOutcomeType.Exit);
                return;
            }

            _view?.Show();
            SetupPortrait(assignment.Npc);
            AdvanceAndCompose();
        }

        public void StartDialogue(string dialogueKnot, string speakerName = null)
        {
            if (string.IsNullOrEmpty(dialogueKnot))
            {
                Debug.LogError("[CompositeDialoguePresenter] Cannot start dialogue: no knot specified");
                return;
            }

            _currentAssignment = null;
            _npcActive = false;
            _storyActive = true;

            _model.StartDialogue(null, speakerName);
            _storyManager.GoToKnot(dialogueKnot);

            _view?.Show();
            _view?.ClearPortrait();
            AdvanceAndCompose();
        }

        public void ContinueDialogue()
        {
            if (!_model.IsActive)
                return;

            if (_isTypewriting)
            {
                _view?.SkipTypewriterEffect();
                _isTypewriting = false;
                ShowDialogueText(_pendingText);
                return;
            }

            AdvanceAndCompose();
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!_model.IsActive || !_model.HasChoices)
                return;

            if (choiceIndex < 0 || choiceIndex >= _choiceSources.Count)
            {
                Debug.LogError($"[CompositeDialoguePresenter] Invalid choice index: {choiceIndex}");
                return;
            }

            var source = _choiceSources[choiceIndex];

            if (source.IsStory)
            {
                _storyManager.ChooseChoice(source.OriginalIndex);
            }
            else
            {
                _npcManager.ChooseChoice(source.OriginalIndex);
            }

            _view?.HideChoices();
            AdvanceAndCompose();
        }

        public void EndDialogue(DialogueOutcomeType outcome)
        {
            _model.EndDialogue(outcome);
            _view?.Hide();
            _currentAssignment = null;
            _storyActive = false;
            _npcActive = false;
            _choiceSources.Clear();

            OnDialogueEnded?.Invoke(outcome);
        }

        public void FireCombatTriggered(string enemyId)
        {
            OnCombatTriggered?.Invoke(enemyId);
        }

        public void Dispose()
        {
            UnsubscribeFromViewEvents();
            UnsubscribeFromBinderEvents();
            _model.Clear();
        }

        /// <summary>
        /// Advances both story managers and composes their output into unified display.
        /// </summary>
        private void AdvanceAndCompose()
        {
            string text = null;
            _pendingOutcome = null;

            // Priority: advance story first, then NPC
            if (_storyActive && _storyManager.CanContinue)
            {
                text = _storyManager.Continue();
                ProcessTags(_storyManager.CurrentTags);
            }
            else if (_npcActive && _npcManager.CanContinue)
            {
                text = _npcManager.Continue();
                ProcessNpcTags(_npcManager.CurrentTags);
            }

            // Handle pending outcome AFTER all tag processing is complete
            if (_pendingOutcome.HasValue)
            {
                EndDialogue(_pendingOutcome.Value);
                return;
            }

            // Check if both stories have ended
            bool storyDone = !_storyActive || (!_storyManager.CanContinue && !_storyManager.HasChoices);
            bool npcDone = !_npcActive || (!_npcManager.CanContinue && !_npcManager.HasChoices);

            if (storyDone && npcDone && text == null)
            {
                EndDialogue(DialogueOutcomeType.Continue);
                return;
            }

            if (text != null)
            {
                ShowDialogueText(text);
            }

            ComposeChoices();
            UpdateContinueButton();
        }

        /// <summary>
        /// Combines choices from both story managers into one unified list.
        /// </summary>
        private void ComposeChoices()
        {
            _choiceSources.Clear();
            var combined = new List<DialogueChoice>();
            int displayIndex = 0;

            // Story choices first
            if (_storyActive && _storyManager.HasChoices)
            {
                var storyChoices = _storyManager.CurrentChoices;
                for (int i = 0; i < storyChoices.Count; i++)
                {
                    var sc = storyChoices[i];
                    combined.Add(new DialogueChoice(displayIndex, sc.Text, true, sc.Tags));
                    _choiceSources.Add(new ChoiceSource(true, i));
                    displayIndex++;
                }
            }

            // NPC character choices
            if (_npcActive && _npcManager.HasChoices)
            {
                var npcChoices = _npcManager.CurrentChoices;
                for (int i = 0; i < npcChoices.Count; i++)
                {
                    var nc = npcChoices[i];
                    combined.Add(new DialogueChoice(displayIndex, nc.Text, true, nc.Tags));
                    _choiceSources.Add(new ChoiceSource(false, i));
                    displayIndex++;
                }
            }

            if (combined.Count > 0)
            {
                _model.SetChoices(combined);
                _view?.ShowChoices(combined);
                _view?.HideContinueButton();
            }
            else
            {
                _model.SetChoices(Array.Empty<DialogueChoice>());
                _view?.HideChoices();
            }
        }

        private void UpdateContinueButton()
        {
            bool canContinue = (_storyActive && _storyManager.CanContinue)
                || (_npcActive && _npcManager.CanContinue);

            _model.SetCanContinue(canContinue);

            if (!_model.HasChoices)
            {
                _view?.ShowContinueButton();
            }
            else
            {
                _view?.HideContinueButton();
            }
        }

        private void ProcessTags(IReadOnlyList<string> tags)
        {
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
                ProcessTag(tags[i]);
        }

        private void ProcessNpcTags(IReadOnlyList<string> tags)
        {
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
                ProcessNpcTag(tags[i]);
        }

        private void ProcessTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            var colonIndex = tag.IndexOf(':');
            if (colonIndex <= 0) return;

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

        private void ProcessNpcTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            var colonIndex = tag.IndexOf(':');
            if (colonIndex <= 0) return;

            var key = tag.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = tag.Substring(colonIndex + 1).Trim();

            if (key == "speaker")
            {
                // "self" maps to NPC's display name
                var speakerName = value.Equals("self", StringComparison.OrdinalIgnoreCase)
                    ? _currentAssignment?.Npc.DisplayName ?? "NPC"
                    : value;

                _model.SetSpeaker(speakerName);
                _view?.SetSpeakerName(speakerName);
            }
        }

        private void HandleOutcomeTag(string outcome)
        {
            switch (outcome.ToLowerInvariant())
            {
                case "combat":
                    _pendingOutcome = DialogueOutcomeType.Combat;
                    break;
                case "quest":
                    _pendingOutcome = DialogueOutcomeType.Quest;
                    break;
                case "trade":
                    _pendingOutcome = DialogueOutcomeType.Trade;
                    break;
                case "exit":
                    _pendingOutcome = DialogueOutcomeType.Exit;
                    break;
            }
        }

        private void ShowDialogueText(string text)
        {
            _model.SetText(text);
            _view?.SetDialogueText(text);
        }

        private void SetupPortrait(NpcDefinition npc)
        {
            if (npc?.Portrait != null)
                _view?.SetPortrait(npc.Portrait);
            else
                _view?.ClearPortrait();
        }

        private void SubscribeToViewEvents()
        {
            if (_view == null) return;
            _view.OnContinueClicked += HandleContinueClicked;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.OnSkipRequested += HandleSkipRequested;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (_view == null) return;
            _view.OnContinueClicked -= HandleContinueClicked;
            _view.OnChoiceSelected -= HandleChoiceSelected;
            _view.OnSkipRequested -= HandleSkipRequested;
        }

        private void SubscribeToBinderEvents()
        {
            if (_externalFunctionBinder == null) return;
            _externalFunctionBinder.OnCombatRequested += HandleBinderCombatRequested;
        }

        private void UnsubscribeFromBinderEvents()
        {
            if (_externalFunctionBinder == null) return;
            _externalFunctionBinder.OnCombatRequested -= HandleBinderCombatRequested;
        }

        private void HandleContinueClicked() => ContinueDialogue();
        private void HandleChoiceSelected(int index) => SelectChoice(index);
        private void HandleBinderCombatRequested(string enemyId) => FireCombatTriggered(enemyId);

        private void HandleSkipRequested()
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
        /// Tracks which IStoryManager owns a displayed choice.
        /// </summary>
        private readonly struct ChoiceSource
        {
            public bool IsStory { get; }
            public int OriginalIndex { get; }

            public ChoiceSource(bool isStory, int originalIndex)
            {
                IsStory = isStory;
                OriginalIndex = originalIndex;
            }
        }
    }
}
