using System;
using System.Collections.Generic;

namespace Narrative.Dialogue
{
    /// <summary>
    /// View interface for dialogue UI.
    /// Implemented by MonoBehaviour adapters.
    /// </summary>
    public interface IDialogueView
    {
        /// <summary>
        /// Event fired when user clicks continue.
        /// </summary>
        event Action OnContinueClicked;

        /// <summary>
        /// Event fired when user selects a choice.
        /// </summary>
        event Action<int> OnChoiceSelected;

        /// <summary>
        /// Event fired when user requests to skip/exit dialogue.
        /// </summary>
        event Action OnSkipRequested;

        /// <summary>
        /// Shows the dialogue panel.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the dialogue panel.
        /// </summary>
        void Hide();

        /// <summary>
        /// Sets the speaker name.
        /// </summary>
        void SetSpeakerName(string name);

        /// <summary>
        /// Sets the dialogue text.
        /// </summary>
        void SetDialogueText(string text);

        /// <summary>
        /// Sets the speaker portrait.
        /// </summary>
        void SetPortrait(UnityEngine.Sprite portrait);

        /// <summary>
        /// Clears the portrait.
        /// </summary>
        void ClearPortrait();

        /// <summary>
        /// Shows choices for the player to select.
        /// </summary>
        void ShowChoices(IReadOnlyList<DialogueChoice> choices);

        /// <summary>
        /// Hides the choices panel.
        /// </summary>
        void HideChoices();

        /// <summary>
        /// Shows the continue button.
        /// </summary>
        void ShowContinueButton();

        /// <summary>
        /// Hides the continue button.
        /// </summary>
        void HideContinueButton();

        /// <summary>
        /// Enables or disables the skip button.
        /// </summary>
        void SetSkipEnabled(bool enabled);

        /// <summary>
        /// Plays a typewriter effect for the text.
        /// </summary>
        void PlayTypewriterEffect(string text, float charsPerSecond, Action onComplete);

        /// <summary>
        /// Skips the current typewriter effect.
        /// </summary>
        void SkipTypewriterEffect();
    }
}
