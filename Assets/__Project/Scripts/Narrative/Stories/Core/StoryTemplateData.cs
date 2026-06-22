using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Stories.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free story template (R5/R7): a skeleton of typed <see cref="StorySlot"/>s,
    /// fact preconditions that gate eligibility, any story-level direct effects, and director metadata
    /// (tags, thread label, spine flag). It references NO other template (R7).
    ///
    /// The advisory effect footprint is NOT stored here — it is derived by the casting/planning component
    /// that holds the fragment library (W3-2), and may be cached via <see cref="SetDerivedFootprint"/>.
    /// </summary>
    public sealed class StoryTemplateData
    {
        public string StoryId { get; }
        public IReadOnlyList<StorySlot> Slots { get; }
        public IReadOnlyList<FactPredicate> Preconditions { get; }
        public IReadOnlyList<FactEffectCore> OwnEffects { get; }
        public IReadOnlyList<string> StoryTags { get; }
        public string ThreadId { get; }
        public bool IsSpine { get; }

        /// <summary>
        /// Pacing cost/size used by the windowed director to fill a window's narrative budget. A larger
        /// weight consumes more of the per-window budget, so it appears less often alongside other stories.
        /// Not a difficulty or platform-span measure — a single story is always one platform.
        /// </summary>
        public int Weight { get; }

        private IReadOnlyList<FactKeyShapeCore> _derivedFootprint;

        public StoryTemplateData(string storyId, IReadOnlyList<StorySlot> slots, IReadOnlyList<FactPredicate> preconditions,
            IReadOnlyList<FactEffectCore> ownEffects, IReadOnlyList<string> storyTags, string threadId, bool isSpine,
            int weight = 0)
        {
            StoryId = storyId ?? string.Empty;
            Slots = slots ?? System.Array.Empty<StorySlot>();
            Preconditions = preconditions ?? System.Array.Empty<FactPredicate>();
            OwnEffects = ownEffects ?? System.Array.Empty<FactEffectCore>();
            StoryTags = storyTags ?? System.Array.Empty<string>();
            ThreadId = threadId ?? string.Empty;
            IsSpine = isSpine;
            Weight = weight;
        }

        /// <summary>
        /// Advisory union footprint (own effects + candidate-fragment footprints) used by the director
        /// for planning only — never the runtime write-gate (W2-1/W3-2). Null until the planning
        /// component derives it.
        /// </summary>
        public IReadOnlyList<FactKeyShapeCore> DerivedFootprint => _derivedFootprint;

        public void SetDerivedFootprint(IReadOnlyList<FactKeyShapeCore> footprint)
        {
            _derivedFootprint = footprint;
        }
    }
}
