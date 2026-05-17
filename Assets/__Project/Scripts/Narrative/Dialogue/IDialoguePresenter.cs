using System;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Interface for dialogue presentation and coordination.
    /// Abstracts the DialoguePresenter for dependency inversion.
    /// </summary>
    public interface IDialoguePresenter
    {
        /// <summary>
        /// Event fired when dialogue session ends.
        /// </summary>
        event Action<DialogueOutcomeType> OnDialogueEnded;

        /// <summary>
        /// Event fired when a combat outcome is triggered.
        /// </summary>
        event Action<string> OnCombatTriggered;

        /// <summary>
        /// Event fired when a quest outcome is triggered.
        /// </summary>
        event Action<string> OnQuestTriggered;

        /// <summary>
        /// Whether dialogue is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Starts a dialogue with an NPC.
        /// </summary>
        /// <param name="npcId">The NPC identifier</param>
        /// <param name="dialogueKnot">The Ink knot to start from</param>
        void StartNpcDialogue(string npcId, string dialogueKnot);

        /// <summary>
        /// Starts a dialogue from a specific knot (no NPC context).
        /// </summary>
        /// <param name="dialogueKnot">The Ink knot to start from</param>
        /// <param name="speakerName">Optional speaker name override</param>
        void StartDialogue(string dialogueKnot, string speakerName = null);

        /// <summary>
        /// Continues to the next line of dialogue.
        /// </summary>
        void ContinueDialogue();

        /// <summary>
        /// Selects a choice by index.
        /// </summary>
        /// <param name="choiceIndex">The index of the choice to select</param>
        void SelectChoice(int choiceIndex);

        /// <summary>
        /// Ends the dialogue session with a specific outcome.
        /// </summary>
        /// <param name="outcome">The outcome type</param>
        void EndDialogue(DialogueOutcomeType outcome);

        /// <summary>
        /// Fires the combat triggered event.
        /// Used by external function bindings.
        /// </summary>
        /// <param name="enemyId">The enemy identifier</param>
        void FireCombatTriggered(string enemyId);
    }
}
