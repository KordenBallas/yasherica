using Combat.Battlefield;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// One content kind's size/shape character: how many whole cells the platform grows to, and how
    /// compactly it grows (0 = ragged uniform accretion, <see cref="MaxCompactness"/> = strongly
    /// hugs the blob, i.e. round). Integer dial so the seeded draw stays exact-integer arithmetic.
    /// </summary>
    public sealed class ShapeProfile
    {
        public const int MaxCompactness = 8;

        public int MinCells { get; }
        public int MaxCells { get; }
        public int Compactness { get; }

        public ShapeProfile(int minCells, int maxCells, int compactness)
        {
            MinCells = minCells < 1 ? 1 : minCells;
            MaxCells = maxCells < MinCells ? MinCells : maxCells;
            Compactness = compactness < 0 ? 0 : compactness > MaxCompactness ? MaxCompactness : compactness;
        }
    }

    /// <summary>
    /// Immutable, UnityEngine-free platform size/shape dials (the platform-hex-surface-and-shape
    /// brief): the hex tiling shared with combat, one <see cref="ShapeProfile"/> per content kind,
    /// the battlefield minimum a combat-capable platform must meet, the decorative rim tunables, and
    /// the layout values that used to live on <c>AreaGeneratorConfig</c>. Mapped from the
    /// <c>PlatformShapeConfig</c> SO at install time.
    /// </summary>
    public sealed class PlatformShapeSettings
    {
        public float HexSize { get; }
        public HexOrientation Orientation { get; }

        public ShapeProfile Empty { get; }
        public ShapeProfile Loot { get; }
        public ShapeProfile Combat { get; }
        public ShapeProfile Npc { get; }

        /// <summary>Whole-cell floor for every combat-capable platform (brief §6).</summary>
        public int BattlefieldMinimumCells { get; }

        /// <summary>Base outward width of the decorative rim, world units.</summary>
        public float RimWidth { get; }

        /// <summary>Per-vertex rim width jitter, percent of <see cref="RimWidth"/> (0–100).</summary>
        public int RimJitterPercent { get; }

        /// <summary>How far the rim ring droops below the walkable top, world units.</summary>
        public float RimDropHeight { get; }

        public float PlatformThickness { get; }
        public float GapBetweenPlatforms { get; }

        /// <summary>Per-cell top inset feeding the bevel seams (muted traversal tiling); 0 = flat.</summary>
        public float CellInset { get; }

        public PlatformShapeSettings(
            float hexSize,
            HexOrientation orientation,
            ShapeProfile empty,
            ShapeProfile loot,
            ShapeProfile combat,
            ShapeProfile npc,
            int battlefieldMinimumCells,
            float rimWidth,
            int rimJitterPercent,
            float rimDropHeight,
            float platformThickness,
            float gapBetweenPlatforms,
            float cellInset)
        {
            HexSize = hexSize <= 0f ? DefaultHexSize : hexSize;
            Orientation = orientation;
            Empty = empty ?? DefaultEmptyProfile();
            Loot = loot ?? DefaultLootProfile();
            Combat = combat ?? DefaultCombatProfile();
            Npc = npc ?? DefaultNpcProfile();
            BattlefieldMinimumCells = battlefieldMinimumCells < 1 ? 1 : battlefieldMinimumCells;
            RimWidth = rimWidth < 0f ? 0f : rimWidth;
            RimJitterPercent = rimJitterPercent < 0 ? 0 : rimJitterPercent > 100 ? 100 : rimJitterPercent;
            RimDropHeight = rimDropHeight < 0f ? 0f : rimDropHeight;
            PlatformThickness = platformThickness <= 0f ? DefaultPlatformThickness : platformThickness;
            GapBetweenPlatforms = gapBetweenPlatforms < 0f ? 0f : gapBetweenPlatforms;
            CellInset = cellInset < 0f ? 0f : cellInset;
        }

        public ShapeProfile ProfileFor(PlatformContentKind kind)
        {
            switch (kind)
            {
                case PlatformContentKind.Loot: return Loot;
                case PlatformContentKind.Combat: return Combat;
                case PlatformContentKind.Npc: return Npc;
                default: return Empty;
            }
        }

        // Defaults double as the mapper fallback when no config asset is wired.
        public const float DefaultHexSize = 2f;
        public const HexOrientation DefaultOrientation = HexOrientation.Flat;
        public const int DefaultBattlefieldMinimumCells = 12;
        public const float DefaultRimWidth = 1.2f;
        public const int DefaultRimJitterPercent = 35;
        public const float DefaultRimDropHeight = 0.4f;
        public const float DefaultPlatformThickness = 1f;
        public const float DefaultGapBetweenPlatforms = 2f;
        public const float DefaultCellInset = 0.06f;

        public static ShapeProfile DefaultEmptyProfile() => new ShapeProfile(2, 4, 3);
        public static ShapeProfile DefaultLootProfile() => new ShapeProfile(3, 5, 3);
        public static ShapeProfile DefaultCombatProfile() => new ShapeProfile(12, 18, 6);
        public static ShapeProfile DefaultNpcProfile() => new ShapeProfile(4, 7, 3);

        public static PlatformShapeSettings CreateDefault()
        {
            return new PlatformShapeSettings(
                DefaultHexSize, DefaultOrientation,
                DefaultEmptyProfile(), DefaultLootProfile(), DefaultCombatProfile(), DefaultNpcProfile(),
                DefaultBattlefieldMinimumCells,
                DefaultRimWidth, DefaultRimJitterPercent, DefaultRimDropHeight,
                DefaultPlatformThickness, DefaultGapBetweenPlatforms,
                DefaultCellInset);
        }
    }
}
