using UnityEngine;

namespace Narrative.View
{
    /// <summary>
    /// Belonging id → belonging colour for the card grammar (P0-3·b): a race id resolves to the
    /// authored race colour (a Part-Blank reward), a reward-family id to the authored family colour
    /// (an artifact reward). One lookup so the offer card never cares which kind the belonging is.
    /// </summary>
    public interface IBelongingTintCatalog
    {
        /// <summary>The authored colour for a belonging id; white for unknown/empty (neutral tint).</summary>
        Color TintFor(string belongingId);
    }
}
