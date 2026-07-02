using LevelGeneration.Surface;

namespace LevelGeneration.Data
{
    /// <summary>
    /// The only bridge from the <see cref="PlatformShapeConfig"/> SO to the UnityEngine-free
    /// <see cref="PlatformShapeSettings"/> Core record (CLAUDE.md §7). Falls back to the same
    /// defaults as the SO when no config asset is wired.
    /// </summary>
    public static class PlatformShapeConfigMapper
    {
        public static PlatformShapeSettings ToSettings(PlatformShapeConfig config)
        {
            if (config == null)
            {
                return PlatformShapeSettings.CreateDefault();
            }

            return new PlatformShapeSettings(
                config.HexCellSize,
                config.Orientation,
                ToProfile(config.EmptyProfile),
                ToProfile(config.LootProfile),
                ToProfile(config.CombatProfile),
                ToProfile(config.NpcProfile),
                config.BattlefieldMinimumCells,
                config.RimWidth,
                config.RimJitterPercent,
                config.RimDropHeight,
                config.PlatformThickness,
                config.GapBetweenPlatforms,
                config.HeightDeviation,
                config.CellInset);
        }

        private static ShapeProfile ToProfile(PlatformShapeConfig.ShapeProfileData data)
        {
            if (data == null)
            {
                return null; // PlatformShapeSettings substitutes its per-kind default.
            }

            return new ShapeProfile(data.MinCells, data.MaxCells, data.Compactness);
        }
    }
}
