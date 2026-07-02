using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// Renders a mini 3D model of the hero wearing an offered part, for the mutation
    /// card's hover popover. Presentation-layer service: the choice view requests a
    /// preview per hovered card; the presenter never sees it.
    /// </summary>
    public interface IMutationModelPreview
    {
        /// <summary>
        /// Starts rendering the hero with <paramref name="partId"/> in <paramref name="slotId"/>.
        /// False when no assembled hero exists yet (the popover simply does not open).
        /// </summary>
        bool TryShow(string slotId, string partId, out Texture texture);

        /// <summary>Stops rendering and releases the preview model.</summary>
        void Hide();
    }
}
