using System;
using System.Collections.Generic;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Pure C# model representing the current dialogue state.
    /// Contains no Unity dependencies - fully testable.
    /// </summary>
    public class DialogueModel
    {
        /// <summary>
        /// Current speaker name.
        /// </summary>
        public string SpeakerName { get; private set; }

        /// <summary>
        /// Current dialogue text.
        /// </summary>
        public string CurrentText { get; private set; }

        /// <summary>
        /// Available choices for the current dialogue node.
        /// </summary>
        public IReadOnlyList<DialogueChoice> Choices { get; private set; }

        /// <summary>
        /// Whether the dialogue is currently active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Whether there are choices available.
        /// </summary>
        public bool HasChoices => Choices != null && Choices.Count > 0;

        /// <summary>
        /// Whether the dialogue can continue (text remaining).
        /// </summary>
        public bool CanContinue { get; private set; }

        /// <summary>
        /// NPC ID if dialogue is with an NPC.
        /// </summary>
        public string NpcId { get; private set; }

        /// <summary>
        /// Portrait identifier for the current speaker.
        /// </summary>
        public string PortraitId { get; private set; }

        /// <summary>
        /// Current dialogue outcome (set when dialogue ends).
        /// </summary>
        public DialogueOutcomeType Outcome { get; private set; }

        /// <summary>
        /// Event fired when the model state changes.
        /// </summary>
        public event Action OnStateChanged;

        public DialogueModel()
        {
            Choices = Array.Empty<DialogueChoice>();
            Outcome = DialogueOutcomeType.Continue;
        }

        /// <summary>
        /// Starts a dialogue session.
        /// </summary>
        public void StartDialogue(string npcId = null, string speakerName = null)
        {
            IsActive = true;
            NpcId = npcId;
            SpeakerName = speakerName ?? string.Empty;
            CurrentText = string.Empty;
            Choices = Array.Empty<DialogueChoice>();
            CanContinue = true;
            Outcome = DialogueOutcomeType.Continue;

            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the current dialogue text.
        /// </summary>
        public void SetText(string text, string speakerName = null)
        {
            CurrentText = text ?? string.Empty;
            if (speakerName != null)
            {
                SpeakerName = speakerName;
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the speaker information.
        /// </summary>
        public void SetSpeaker(string name, string portraitId = null)
        {
            SpeakerName = name ?? string.Empty;
            PortraitId = portraitId;

            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the available choices.
        /// </summary>
        public void SetChoices(IReadOnlyList<DialogueChoice> choices)
        {
            Choices = choices ?? Array.Empty<DialogueChoice>();
            NotifyStateChanged();
        }

        /// <summary>
        /// Updates whether the dialogue can continue.
        /// </summary>
        public void SetCanContinue(bool canContinue)
        {
            CanContinue = canContinue;
            NotifyStateChanged();
        }

        /// <summary>
        /// Ends the dialogue with the specified outcome.
        /// </summary>
        public void EndDialogue(DialogueOutcomeType outcome)
        {
            IsActive = false;
            Outcome = outcome;
            CurrentText = string.Empty;
            Choices = Array.Empty<DialogueChoice>();
            CanContinue = false;

            NotifyStateChanged();
        }

        /// <summary>
        /// Clears all dialogue state.
        /// </summary>
        public void Clear()
        {
            IsActive = false;
            NpcId = null;
            SpeakerName = string.Empty;
            PortraitId = null;
            CurrentText = string.Empty;
            Choices = Array.Empty<DialogueChoice>();
            CanContinue = false;
            Outcome = DialogueOutcomeType.Continue;

            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Represents a choice in the dialogue.
    /// </summary>
    public class DialogueChoice
    {
        public int Index { get; }
        public string Text { get; }
        public bool IsEnabled { get; }
        public IReadOnlyList<string> Tags { get; }

        public DialogueChoice(int index, string text, bool isEnabled = true, IReadOnlyList<string> tags = null)
        {
            Index = index;
            Text = text;
            IsEnabled = isEnabled;
            Tags = tags ?? Array.Empty<string>();
        }
    }
}
