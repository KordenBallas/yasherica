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

        /// <summary>Shows or hides the F interaction prompt.</summary>
        void ShowPrompt(bool visible);

        /// <summary>Destroys the view GameObject.</summary>
        void DestroyView();
    }
}
