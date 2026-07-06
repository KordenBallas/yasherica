using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using World.Biomes.Data;
using World.Dressing.Core;

namespace World.Dressing.Data
{
    /// <summary>
    /// The one SO → Core bridge of the dressing system (install/generation time only): maps the
    /// biome appearance assets' kit bindings to the pure feature-pool catalog and the site kit
    /// assets to the pure site catalog. Entry indices stay aligned with the SO lists — a broken
    /// entry (null prefab) keeps its slot at weight 0 so swapping a kit never shifts a draw.
    /// </summary>
    public static class DressingKitMapper
    {
        public static IBiomeFeaturePoolCatalog ToBiomeCatalog(
            IReadOnlyList<BiomeAppearanceDefinition> appearances, IGameLogger logger = null)
        {
            var pools = new Dictionary<LevelTheme, (FeaturePoolData, FeatureDensitySettings)>();
            if (appearances == null)
            {
                return new BiomeFeaturePoolCatalog(pools);
            }

            foreach (var appearance in appearances)
            {
                if (appearance == null || appearance.FeatureKit == null)
                {
                    continue;
                }

                if (pools.ContainsKey(appearance.Theme))
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[DressingKitMapper] Duplicate biome appearance for '{appearance.Theme}' — first wins.");
                    continue;
                }

                pools[appearance.Theme] = (
                    ToPool(appearance.FeatureKit, logger),
                    new FeatureDensitySettings(
                        appearance.BlockersPer100Cells,
                        appearance.DecorClustersPer100Cells,
                        appearance.LaneHalfWidthCells));
            }

            return new BiomeFeaturePoolCatalog(pools);
        }

        public static ISiteDressingCatalog ToSiteCatalog(
            IReadOnlyList<SiteDressingKitDefinition> kits, IGameLogger logger = null)
        {
            var mapped = new Dictionary<string, SiteKitData>();
            if (kits == null)
            {
                return new SiteDressingCatalog(mapped);
            }

            foreach (var kit in kits)
            {
                if (kit == null || string.IsNullOrEmpty(kit.DressingThemeId))
                {
                    continue;
                }

                if (mapped.ContainsKey(kit.DressingThemeId))
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[DressingKitMapper] Duplicate site kit for theme id '{kit.DressingThemeId}' — first wins.");
                    continue;
                }

                mapped[kit.DressingThemeId] = new SiteKitData(
                    kit.DressingThemeId,
                    kit.Structures.Count,
                    kit.Props.Count,
                    kit.FocalProps.Count,
                    kit.GateProps.Count);
            }

            return new SiteDressingCatalog(mapped);
        }

        /// <summary>Maps a backdrop kit's entries to the pure scatter pool (index-aligned; a
        /// broken entry keeps its slot at weight 0 so swapping never shifts a draw).</summary>
        public static IReadOnlyList<BackdropEntryData> ToBackdropEntries(
            BackdropKitDefinition kit, IGameLogger logger = null)
        {
            if (kit == null)
            {
                return System.Array.Empty<BackdropEntryData>();
            }

            var entries = new List<BackdropEntryData>(kit.Entries.Count);
            for (int i = 0; i < kit.Entries.Count; i++)
            {
                var entry = kit.Entries[i];
                if (entry == null || entry.Prefab == null)
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[DressingKitMapper] Backdrop kit '{kit.KitId}' entry {i} has no prefab — kept at weight 0.");
                    entries.Add(new BackdropEntryData(0, 1f, 1f));
                    continue;
                }

                entries.Add(new BackdropEntryData(entry.Weight, entry.ScaleMin, entry.ScaleMax));
            }

            return entries;
        }

        private static FeaturePoolData ToPool(BiomeFeatureKitDefinition kit, IGameLogger logger)
        {
            var entries = new List<FeatureEntryData>(kit.Features.Count);
            for (int i = 0; i < kit.Features.Count; i++)
            {
                var entry = kit.Features[i];
                if (entry == null || entry.Prefab == null)
                {
                    logger?.Warning(LogCategory.LevelGeneration,
                        $"[DressingKitMapper] Kit '{kit.KitId}' feature entry {i} has no prefab — kept at weight 0.");
                    entries.Add(new FeatureEntryData(FeatureKind.SmallDecorative, 0, 1f, 1f));
                    continue;
                }

                entries.Add(new FeatureEntryData(
                    entry.Kind, entry.Weight, entry.ScaleMin, entry.ScaleMax,
                    entry.FootprintOverride, entry.MayOverhang));
            }

            return new FeaturePoolData(kit.KitId, entries);
        }
    }
}
