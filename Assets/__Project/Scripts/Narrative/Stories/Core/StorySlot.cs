using System.Collections.Generic;

namespace Narrative.Stories.Core
{
    /// <summary>Kind of fragment a story slot accepts.</summary>
    public enum SlotKind
    {
        Dialogue = 0,
        Quest = 1,
        Combat = 2
    }

    /// <summary>
    /// Immutable typed slot in a story template (R5): a kind plus the semantic tags a fragment must
    /// carry to fill it. Fragments are matched by these tags, never by hard reference. Optional slots
    /// may be left unfilled.
    /// </summary>
    public sealed class StorySlot
    {
        public string SlotId { get; }
        public SlotKind Kind { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public bool Optional { get; }

        public StorySlot(string slotId, SlotKind kind, IReadOnlyList<string> requiredTags, bool optional)
        {
            SlotId = slotId ?? string.Empty;
            Kind = kind;
            RequiredTags = requiredTags ?? System.Array.Empty<string>();
            Optional = optional;
        }
    }
}
