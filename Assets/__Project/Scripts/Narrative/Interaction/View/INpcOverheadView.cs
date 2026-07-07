using Narrative.Interaction.Core;

namespace Narrative.Interaction.View
{
    /// <summary>
    /// The overhead UI an NPC carries: an always-visible name label and intent marker, plus an in-range
    /// F prompt. A thin view (MVP) — the presenter decides what to show; the implementation only renders
    /// and billboards to the camera.
    /// </summary>
    public interface INpcOverheadView
    {
        /// <summary>Sets the always-visible name shown above the model.</summary>
        void SetName(string displayName);

        /// <summary>Shows the floating intent marker: <c>?</c> for quest-bearer, <c>!</c> for hostile, none for plain.</summary>
        void SetIntentMarker(NpcIntent intent);

        /// <summary>Shows or hides the interaction prompt.</summary>
        void ShowPrompt(bool visible);

        /// <summary>Sets the prompt text (e.g. "[F] Talk" / "[Y] Talk") — the presenter composes it
        /// from the active input source's cue, so it re-renders on device switch.</summary>
        void SetPromptText(string text);

        /// <summary>Destroys the view GameObject.</summary>
        void DestroyView();
    }
}
