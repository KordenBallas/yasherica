using System;
using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration;

namespace World.Dressing.Core
{
    /// <summary>
    /// The deterministic dressing outcome for one platform: which cells its blocking features
    /// consume and where every dressing instance sits. Built by the pure planners from the
    /// per-platform seed; the Unity side only instantiates it. Immutable; pure C#.
    /// </summary>
    public sealed class PlatformDressingPlan
    {
        /// <summary>The fail-safe base layer: no blocked cells, nothing placed (brief FR8).</summary>
        public static readonly PlatformDressingPlan Empty = new PlatformDressingPlan(
            DressingPlanKind.None, string.Empty, default,
            Array.Empty<HexCoordinates>(), Array.Empty<DressingPlacement>());

        public PlatformDressingPlan(
            DressingPlanKind kind,
            string kitKey,
            LevelTheme theme,
            IReadOnlyCollection<HexCoordinates> blockedCells,
            IReadOnlyList<DressingPlacement> placements)
        {
            Kind = kind;
            KitKey = kitKey ?? string.Empty;
            Theme = theme;
            BlockedCells = blockedCells ?? Array.Empty<HexCoordinates>();
            Placements = placements ?? Array.Empty<DressingPlacement>();
        }

        public DressingPlanKind Kind { get; }

        /// <summary>The kit lookup key: kit id for biome plans, dressing-theme id for site plans.</summary>
        public string KitKey { get; }

        /// <summary>The biome the plan was made under — the tone treatment's key at spawn time.</summary>
        public LevelTheme Theme { get; }

        /// <summary>Cells the blocking features consume; folded into the platform surface.</summary>
        public IReadOnlyCollection<HexCoordinates> BlockedCells { get; }

        public IReadOnlyList<DressingPlacement> Placements { get; }

        public bool IsEmpty => Kind == DressingPlanKind.None
            || (BlockedCells.Count == 0 && Placements.Count == 0);
    }
}
