using System;
using Narrative.Generation;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Interface for dialogue presentation and coordination.
    /// Supports dual Ink composition via NpcAssignment.
    /// </summary>
    public interface IDialoguePresenter
    {
        event Action<DialogueOutcomeType> OnDialogueEnded;
        event Action<string> OnCombatTriggered;
        event Action<string> OnQuestTriggered;

        bool IsActive { get; }

        /// <summary>
        /// Starts a composite dialogue from an NpcAssignment (story + character Ink).
        /// </summary>
        void StartDialogue(NpcAssignment assignment);

        /// <summary>
        /// Starts a dialogue from a specific knot (no NPC context).
        /// </summary>
        void StartDialogue(string dialogueKnot, string speakerName = null);

        void ContinueDialogue();
        void SelectChoice(int choiceIndex);
        void EndDialogue(DialogueOutcomeType outcome);
        void FireCombatTriggered(string enemyId);
    }
}
