using Narrative.Actors.Core;
using Narrative.Stories.Core;
using World.Sites.Core;

namespace Narrative.Director.Core
{
    /// <summary>What a planned platform carries.</summary>
    public enum PlannedPlatformKind
    {
        /// <summary>An empty/traversal platform — the deliberate, budgeted majority of the world.</summary>
        Empty = 0,
        /// <summary>A story encounter (with an assigned actor); may also be combat-bearing.</summary>
        Story = 1,
        /// <summary>An ambient aggressive monster from the biome pool — no dialogue or quest attached.</summary>
        Combat = 2,
        /// <summary>A simple low-tier loot find rolled from the biome platform table.</summary>
        Loot = 3
    }

    /// <summary>
    /// One platform slot the windowed director committed for a window (R6/R7 — chosen against the live
    /// facts when the window was planned). Pure C#: the area generator maps this to a graph node and the
    /// entry adapter runs the encounter. For a <see cref="PlannedPlatformKind.Story"/> the
    /// <see cref="Actor"/> is minted once at plan time so its per-actor facts (<c>$self</c>) are stable
    /// for the whole run (D2/R12). For a <see cref="PlannedPlatformKind.Combat"/> the
    /// <see cref="EnemyId"/> is the biome-pool pick made at plan time. <see cref="Flavor"/> and
    /// <see cref="Site"/> carry the beat's <c>base·flavor</c> refinement and site membership (the
    /// world-sites brief); both default on the Wild path.
    /// </summary>
    public sealed class PlannedPlatform
    {
        public PlannedPlatformKind Kind { get; }
        public StoryTemplateData Story { get; }
        public NpcInstance Actor { get; }

        /// <summary>True when the platform hosts combat: an ambient monster, or a story with a combat slot.</summary>
        public bool IsCombat { get; }

        /// <summary>The ambient enemy id (a real id may be 0); meaningful only when <see cref="Kind"/> is Combat.</summary>
        public int EnemyId { get; }

        /// <summary>The beat's flavor tag (e.g. "market", "townsfolk"); empty when unflavored.</summary>
        public string Flavor { get; }

        /// <summary>The platform's site membership; <see cref="SiteStamp.Wild"/> outside a site block.</summary>
        public SiteStamp Site { get; }

        private PlannedPlatform(PlannedPlatformKind kind, StoryTemplateData story, NpcInstance actor,
            bool isCombat, int enemyId, string flavor, SiteStamp site)
        {
            Kind = kind;
            Story = story;
            Actor = actor;
            IsCombat = isCombat;
            EnemyId = enemyId;
            Flavor = flavor ?? string.Empty;
            Site = site;
        }

        public static PlannedPlatform StoryEncounter(StoryTemplateData story, NpcInstance actor, bool isCombat,
            string flavor = null, SiteStamp site = default) =>
            new PlannedPlatform(PlannedPlatformKind.Story, story, actor, isCombat, 0, flavor, site);

        public static PlannedPlatform AmbientCombat(int enemyId, string flavor = null, SiteStamp site = default) =>
            new PlannedPlatform(PlannedPlatformKind.Combat, null, null, true, enemyId, flavor, site);

        public static PlannedPlatform LootDrop(string flavor = null, SiteStamp site = default) =>
            new PlannedPlatform(PlannedPlatformKind.Loot, null, null, false, 0, flavor, site);

        public static PlannedPlatform EmptyFiller(SiteStamp site = default) =>
            new PlannedPlatform(PlannedPlatformKind.Empty, null, null, false, 0, null, site);
    }
}
