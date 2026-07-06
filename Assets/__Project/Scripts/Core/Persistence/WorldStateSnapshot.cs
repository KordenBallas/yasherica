using System;
using System.Collections.Generic;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// One realized platform of a generated window, as committed at plan time (D5): windows behind
    /// the player are NEVER re-planned on restore — re-planning against end-state facts is not
    /// reproducible and re-casting would consume RNG draws that already happened. Story nodes carry
    /// their casting by id; a <see cref="Consumed"/> node (already resolved behind the player)
    /// restores content-free so nothing respawns.
    /// </summary>
    [Serializable]
    public class WindowNodeSnapshot
    {
        public int NodeId;
        public int Kind; // PlannedPlatformKind
        public bool IsCombat; // story nodes may be combat-bearing
        public int EnemyId; // ambient-combat pick (meaningful when Kind is Combat)
        public List<int> CrewEnemyIds = new List<int>(); // camp crew (meaningful when Kind is Camp)
        public string ContentFlavor = string.Empty;
        public string SiteId = string.Empty;
        public int SiteInstanceId;
        public int SiteIndex;
        public int SiteFootprint;
        public string SiteDressingThemeId = string.Empty;
        public int ShapeKind; // PlatformContentKind the surface was grown with — pins geometry on restore
        public CastingSnapshot Casting; // story nodes only; null otherwise
        public bool Consumed;
    }

    /// <summary>One generated window's realized nodes, in append order.</summary>
    [Serializable]
    public class WindowSnapshot
    {
        public int WindowIndex;
        public List<WindowNodeSnapshot> Nodes = new List<WindowNodeSnapshot>();
    }

    /// <summary>A queued (not yet emitted) site-block slot of the site-aware allocator.</summary>
    [Serializable]
    public class PendingSiteSlotDto
    {
        public int BeatKind; // ContentBaseKind
        public string Flavor = string.Empty;
        public string SiteId = string.Empty;
        public int SiteInstanceId;
        public int SiteIndex;
        public int SiteFootprint;
        public string SiteDressingThemeId = string.Empty;
    }

    /// <summary>The site-aware allocator's run-scoped cursors (world-sites brief).</summary>
    [Serializable]
    public class SiteAllocatorSnapshot
    {
        public List<PendingSiteSlotDto> PendingSlots = new List<PendingSiteSlotDto>();
        public int PlatformsSinceSite;
        public int NextSiteInstanceId = 1;
    }

    /// <summary>
    /// The generated world as one save section (FR4 "the same world behind and ahead"): every
    /// realized window, the streaming cursors, the allocator cursors, and where the hero stands.
    /// The biome journey needs no state here — it is seeded and idempotent per window, so replaying
    /// ApplyForWindow(0..k) during restore reconstructs it exactly.
    /// </summary>
    [Serializable]
    public class WorldStateSnapshot
    {
        public List<WindowSnapshot> Windows = new List<WindowSnapshot>();
        public int WindowIndex;
        public int NextNodeId;
        public List<int> FrontierNodeIds = new List<int>();
        public int CurrentPlatformNodeId;
        public int PlatformsSinceQuest; // WorldContentAllocator quest-spacing cursor
        public SiteAllocatorSnapshot SiteAllocator = new SiteAllocatorSnapshot();
    }
}
