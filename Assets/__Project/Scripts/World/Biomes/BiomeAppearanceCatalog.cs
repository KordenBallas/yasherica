using System.Collections.Generic;
using Core.Logging;
using LevelGeneration;
using World.Biomes.Data;

namespace World.Biomes
{
    /// <summary>
    /// Theme-keyed lookup over the authored BiomeAppearanceDefinition assets. Duplicate themes are
    /// ignored with a warning (first authored wins), matching the biome loot/monster catalogs.
    /// </summary>
    public class BiomeAppearanceCatalog : IBiomeAppearanceCatalog
    {
        private readonly Dictionary<LevelTheme, BiomeAppearanceDefinition> _biomes =
            new Dictionary<LevelTheme, BiomeAppearanceDefinition>();

        public BiomeAppearanceCatalog(IEnumerable<BiomeAppearanceDefinition> definitions, IGameLogger logger)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (_biomes.ContainsKey(definition.Theme))
                {
                    logger?.Warning(
                        LogCategory.LevelGeneration,
                        $"[BiomeAppearanceCatalog] Duplicate biome appearance for theme " +
                        $"{definition.Theme} ('{definition.name}') ignored.");
                    continue;
                }

                _biomes.Add(definition.Theme, definition);
            }
        }

        public BiomeAppearanceDefinition Get(LevelTheme theme)
        {
            return _biomes.TryGetValue(theme, out var definition) ? definition : null;
        }
    }
}
