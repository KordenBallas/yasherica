using World.Sites.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// The content kinds a platform slot can be allocated as (the world content vocabulary,
    /// <c>design/world/content-kinds.md</c>): Empty/traversal, Loot, Combat, the rare NPC quest slot,
    /// and a non-quest NPC beat (a site fill such as NPC·townsfolk — talkable colour, never a quest).
    /// </summary>
    public enum WorldSlotKind
    {
        /// <summary>Traversal breath — the deliberate, budgeted majority of the world.</summary>
        Empty = 0,
        /// <summary>A simple low-tier loot find (the biome platform table).</summary>
        Loot = 1,
        /// <summary>An ambient aggressive monster from the biome pool; no quest or dialogue.</summary>
        Combat = 2,
        /// <summary>A rare NPC story slot — the planner selects an eligible story for it.</summary>
        Quest = 3,
        /// <summary>A non-quest NPC beat (site fill): the planner picks a story by the slot's flavor.</summary>
        Npc = 4,
        /// <summary>A boss-led camp anchor: a boss NPC (cast by the slot's flavor) fronting a crew of enemies.</summary>
        Camp = 5
    }

    /// <summary>
    /// One allocated slot: its kind, the chosen enemy id when the kind is Combat, plus the
    /// <c>base·flavor</c> refinement and site membership when a site block claimed the slot
    /// (both default — empty flavor, Wild stamp — on the plain density path).
    /// </summary>
    public readonly struct SlotAllocation
    {
        public SlotAllocation(WorldSlotKind kind, int enemyId = 0, string flavor = null,
            SiteStamp site = default, System.Collections.Generic.IReadOnlyList<int> crewEnemyIds = null)
        {
            Kind = kind;
            EnemyId = enemyId;
            Flavor = flavor ?? string.Empty;
            Site = site;
            CrewEnemyIds = crewEnemyIds ?? System.Array.Empty<int>();
        }

        public WorldSlotKind Kind { get; }

        /// <summary>The biome-pool enemy id; meaningful only when <see cref="Kind"/> is Combat.</summary>
        public int EnemyId { get; }

        /// <summary>The beat's flavor tag (e.g. "market", "townsfolk"); empty when unflavored.</summary>
        public string Flavor { get; }

        /// <summary>The slot's site membership; <see cref="SiteStamp.Wild"/> outside a site block.</summary>
        public SiteStamp Site { get; }

        /// <summary>The crew's enemy ids (boss's fight); non-empty only when <see cref="Kind"/> is Camp.</summary>
        public System.Collections.Generic.IReadOnlyList<int> CrewEnemyIds { get; }
    }
}
