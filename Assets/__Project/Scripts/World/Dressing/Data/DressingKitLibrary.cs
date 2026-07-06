using System.Collections.Generic;
using Core.Logging;

namespace World.Dressing.Data
{
    /// <summary>
    /// The Unity-side kit lookup the dressing spawner resolves prefabs/materials through: kit id →
    /// biome-feature kit, dressing-theme id → site kit. Built once at install from the same assets
    /// the pure catalogs are mapped from, so plan keys always resolve against matching lists.
    /// </summary>
    public sealed class DressingKitLibrary
    {
        private readonly Dictionary<string, BiomeFeatureKitDefinition> _biomeKits =
            new Dictionary<string, BiomeFeatureKitDefinition>();
        private readonly Dictionary<string, SiteDressingKitDefinition> _siteKits =
            new Dictionary<string, SiteDressingKitDefinition>();

        public DressingKitLibrary(
            IReadOnlyList<BiomeFeatureKitDefinition> biomeKits,
            IReadOnlyList<SiteDressingKitDefinition> siteKits,
            IGameLogger logger = null)
        {
            if (biomeKits != null)
            {
                foreach (var kit in biomeKits)
                {
                    if (kit == null || string.IsNullOrEmpty(kit.KitId))
                    {
                        continue;
                    }

                    if (_biomeKits.ContainsKey(kit.KitId))
                    {
                        logger?.Warning(LogCategory.LevelGeneration,
                            $"[DressingKitLibrary] Duplicate biome kit id '{kit.KitId}' — first wins.");
                        continue;
                    }

                    _biomeKits.Add(kit.KitId, kit);
                }
            }

            if (siteKits != null)
            {
                foreach (var kit in siteKits)
                {
                    if (kit == null || string.IsNullOrEmpty(kit.DressingThemeId))
                    {
                        continue;
                    }

                    if (_siteKits.ContainsKey(kit.DressingThemeId))
                    {
                        logger?.Warning(LogCategory.LevelGeneration,
                            $"[DressingKitLibrary] Duplicate site kit theme id '{kit.DressingThemeId}' — first wins.");
                        continue;
                    }

                    _siteKits.Add(kit.DressingThemeId, kit);
                }
            }
        }

        public BiomeFeatureKitDefinition GetBiomeKit(string kitId)
        {
            return !string.IsNullOrEmpty(kitId) && _biomeKits.TryGetValue(kitId, out var kit) ? kit : null;
        }

        public SiteDressingKitDefinition GetSiteKit(string dressingThemeId)
        {
            return !string.IsNullOrEmpty(dressingThemeId) && _siteKits.TryGetValue(dressingThemeId, out var kit)
                ? kit
                : null;
        }
    }
}
