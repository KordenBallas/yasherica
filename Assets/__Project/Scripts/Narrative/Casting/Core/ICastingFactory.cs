using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// Fills a story template's typed slots with fragments from the library, by tag (R5), to produce a
    /// <see cref="Casting"/> (R3) — the recombination point. Also derives a template's advisory effect
    /// footprint over the library (W3-2), since this is the one component with library access.
    /// </summary>
    public interface ICastingFactory
    {
        /// <summary>
        /// Casts an actor into a story. Returns null if a required slot has no matching fragment.
        /// Records which optional slots were filled so availability vars can be injected (W2-2).
        /// </summary>
        Casting Cast(StoryTemplateData story, NpcInstance actor, IFragmentLibrary library);

        /// <summary>
        /// Computes and caches the story's advisory footprint = union(own-effect shapes, each slot's
        /// matching-fragment footprints), flagging zero-candidate non-optional slots (W3-2).
        /// </summary>
        IReadOnlyList<FactKeyShapeCore> DeriveFootprint(StoryTemplateData story, IFragmentLibrary library);
    }
}
