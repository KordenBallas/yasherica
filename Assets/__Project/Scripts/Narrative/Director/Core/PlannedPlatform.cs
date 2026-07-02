using Narrative.Actors.Core;
using Narrative.Stories.Core;

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
    /// <see cref="EnemyId"/> is the biome-pool pick made at plan time.
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

        private PlannedPlatform(PlannedPlatformKind kind, StoryTemplateData story, NpcInstance actor,
            bool isCombat, int enemyId)
        {
            Kind = kind;
            Story = story;
            Actor = actor;
            IsCombat = isCombat;
            EnemyId = enemyId;
        }

        public static PlannedPlatform StoryEncounter(StoryTemplateData story, NpcInstance actor, bool isCombat) =>
            new PlannedPlatform(PlannedPlatformKind.Story, story, actor, isCombat, 0);

        public static PlannedPlatform AmbientCombat(int enemyId) =>
            new PlannedPlatform(PlannedPlatformKind.Combat, null, null, true, enemyId);

        public static PlannedPlatform LootDrop() =>
            new PlannedPlatform(PlannedPlatformKind.Loot, null, null, false, 0);

        public static PlannedPlatform EmptyFiller() =>
            new PlannedPlatform(PlannedPlatformKind.Empty, null, null, false, 0);
    }
}
