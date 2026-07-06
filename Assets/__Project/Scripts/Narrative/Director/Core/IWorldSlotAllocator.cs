using System.Collections.Generic;
using World.Sites.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// The planner's per-slot allocation seam. The plain density path
    /// (<see cref="WorldContentAllocator"/>) answers "what occupies this platform"; the site-aware
    /// implementation layers block reservation on top: an ambient-channel site claims a run of
    /// upcoming slots, and a quest slot may pull a settlement into being via
    /// <see cref="TryReserveSettlement"/>.
    /// </summary>
    public interface IWorldSlotAllocator
    {
        /// <summary>
        /// Allocates the next platform slot; site-block slots drain before new draws.
        /// <paramref name="currentTier"/> is the run-escalation altitude (D19): combat draws pull only
        /// creatures whose tier band contains it, so the monster pool toughens as the run climbs.
        /// </summary>
        SlotAllocation AllocateSlot(bool questAvailable, int currentTier);

        /// <summary>
        /// Called by the planner once per landed quest slot, with the chosen anchor story's tags.
        /// Decides whether the quest pulls a settlement (a <c>site:&lt;id&gt;</c> tag is a hard
        /// request; otherwise a seeded weighted roll against <c>WildQuestWeight</c>), reserves the
        /// block when it does, and returns the anchor platform's stamp —
        /// <see cref="SiteStamp.Wild"/> when the quest stays a lone wanderer.
        /// </summary>
        SiteStamp TryReserveSettlement(IReadOnlyList<string> anchorStoryTags);
    }
}
