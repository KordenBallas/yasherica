using Narrative.Actors.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>What a planned platform carries.</summary>
    public enum PlannedPlatformKind
    {
        /// <summary>An empty/filler platform (no narrative or combat content).</summary>
        Empty = 0,
        /// <summary>A story encounter (with an assigned actor); may also be combat-bearing.</summary>
        Story = 1,
        /// <summary>A standalone combat encounter with no dialogue (reserved; not emitted in the first pass).</summary>
        Combat = 2,
        /// <summary>A loot-only platform (reserved; loot rolling stays in the area generator for now).</summary>
        Loot = 3
    }

    /// <summary>
    /// One platform slot the windowed director committed for a window (R6/R7 — chosen against the live
    /// facts when the window was planned). Pure C#: the area generator maps this to a graph node and the
    /// entry adapter runs the encounter. For a <see cref="PlannedPlatformKind.Story"/> the
    /// <see cref="Actor"/> is minted once at plan time so its per-actor facts (<c>$self</c>) are stable
    /// for the whole run (D2/R12).
    /// </summary>
    public sealed class PlannedPlatform
    {
        public PlannedPlatformKind Kind { get; }
        public StoryTemplateData Story { get; }
        public NpcInstance Actor { get; }

        /// <summary>True when the chosen story carries a combat slot — counts against the combat budget.</summary>
        public bool IsCombat { get; }

        private PlannedPlatform(PlannedPlatformKind kind, StoryTemplateData story, NpcInstance actor, bool isCombat)
        {
            Kind = kind;
            Story = story;
            Actor = actor;
            IsCombat = isCombat;
        }

        public static PlannedPlatform StoryEncounter(StoryTemplateData story, NpcInstance actor, bool isCombat) =>
            new PlannedPlatform(PlannedPlatformKind.Story, story, actor, isCombat);

        public static PlannedPlatform EmptyFiller() =>
            new PlannedPlatform(PlannedPlatformKind.Empty, null, null, false);
    }
}
