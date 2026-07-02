namespace Narrative.Director.Core
{
    /// <summary>
    /// The four content kinds a platform slot can be allocated as (the world content vocabulary,
    /// <c>design/world/content-kinds.md</c>): Empty/traversal, Loot·scattered, Combat·wild-beast,
    /// NPC·quest-bearer.
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
        Quest = 3
    }

    /// <summary>One allocated slot: its kind, plus the chosen enemy id when the kind is Combat.</summary>
    public readonly struct SlotAllocation
    {
        public SlotAllocation(WorldSlotKind kind, int enemyId = 0)
        {
            Kind = kind;
            EnemyId = enemyId;
        }

        public WorldSlotKind Kind { get; }

        /// <summary>The biome-pool enemy id; meaningful only when <see cref="Kind"/> is Combat.</summary>
        public int EnemyId { get; }
    }
}
