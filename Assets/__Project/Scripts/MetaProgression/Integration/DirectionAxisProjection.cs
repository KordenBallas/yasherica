using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using Inventory.Data;

namespace MetaProgression.Integration
{
    /// <summary>
    /// Projects the content catalogs into the pure id-maps <see cref="Core.DirectionTally"/>
    /// resolves the ledger's raw ids through: part id → race marker, artifact id → function-trait
    /// ids (substance + property). Built once per scene; unknown/removed ids simply drop out of
    /// the maps, keeping the tally robust to content edits.
    /// </summary>
    public static class DirectionAxisProjection
    {
        public static IReadOnlyDictionary<string, string> RaceByPartId(IPartCatalog parts)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (parts?.All == null)
            {
                return map;
            }

            foreach (var part in parts.All)
            {
                if (part != null && !string.IsNullOrEmpty(part.Id) && !string.IsNullOrEmpty(part.RaceId))
                {
                    map[part.Id] = part.RaceId;
                }
            }

            return map;
        }

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> TraitsByArtifactId(
            IArtifactCatalog artifacts)
        {
            var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (artifacts?.All == null)
            {
                return map;
            }

            foreach (var artifact in artifacts.All)
            {
                if (artifact == null || string.IsNullOrEmpty(artifact.Id))
                {
                    continue;
                }

                var traits = new List<string>();
                AddTraitIds(artifact.SubstanceTraits, traits);
                AddTraitIds(artifact.PropertyTraits, traits);
                if (traits.Count > 0)
                {
                    map[artifact.Id] = traits;
                }
            }

            return map;
        }

        private static void AddTraitIds(
            IReadOnlyList<Inventory.Data.Definitions.TraitDefinition> traits, List<string> target)
        {
            if (traits == null)
            {
                return;
            }

            foreach (var trait in traits)
            {
                if (trait != null && !string.IsNullOrEmpty(trait.Id))
                {
                    target.Add(trait.Id);
                }
            }
        }
    }
}
