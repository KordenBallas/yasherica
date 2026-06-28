using System;
using System.Collections.Generic;

namespace Narrative.Encounter
{
    /// <summary>
    /// Adapter contract for the encounter card-hand UI: a plain situation bubble plus a centred hand of
    /// typed cards. Replaces the line-reading + Ink choice-list <c>IDialogueView</c> as the encounter's
    /// presentation surface; the fact/quest/tag engine is unchanged. Implemented by a MonoBehaviour
    /// adapter; driven by <c>EncounterCardHandPresenter</c>.
    /// </summary>
    public interface IEncounterCardHandView
    {
        /// <summary>Raised when the player picks the card at the given hand index.</summary>
        event Action<int> OnCardSelected;

        /// <summary>
        /// Raised when a line has fully revealed — naturally or because a tap snapped it to full. The
        /// presenter decides what follows: a pre-choice line auto-advances into its choices; a closing
        /// reply waits for the dismiss tap (<see cref="OnContinueRequested"/>).
        /// </summary>
        event Action OnRevealCompleted;

        /// <summary>
        /// Raised when the player taps after the line has fully revealed. Used to dismiss the closing
        /// reply and end the encounter; a no-op while a line is still revealing (that tap completes it).
        /// </summary>
        event Action OnContinueRequested;

        /// <summary>Sets the speaker label above the situation bubble.</summary>
        void SetSpeaker(string name);

        /// <summary>
        /// Sets the speaker portrait from the encounter NPC's archetype id (the view resolves the sprite
        /// via <c>INpcArchetypeCatalog</c>). A null/unknown id or a portrait-less archetype falls back to
        /// the neutral placeholder so the box still works (R3). The presenter stays UnityEngine-free.
        /// </summary>
        void SetPortrait(string archetypeId);

        /// <summary>
        /// Shows the current line and starts revealing it word by word at the view's reading speed (R4).
        /// Cards must not be acted on until the reveal completes (R6); the view owns that timing — a tap
        /// mid-reveal completes the line instantly (R5) and a fully-revealed line raises
        /// <see cref="OnRevealCompleted"/>.
        /// </summary>
        void ShowSituation(string line);

        /// <summary>Rebuilds the card hand from the given cards (in order); an empty list clears it.</summary>
        void ShowCards(IReadOnlyList<EncounterCardViewData> cards);

        /// <summary>Shows or hides the whole encounter UI.</summary>
        void SetVisible(bool visible);
    }
}
