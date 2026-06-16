using System;
using System.Collections.Generic;
using Mutation.Core;
using Mutation.Data.Definitions;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Dictionary-backed mutation-option lookup built once from the authored part-set assets.
    /// Fails fast on duplicate or empty archetype ids so authoring mistakes surface at startup
    /// (mirrors <see cref="ArchetypeCatalog"/>). Maps each set into Core options via
    /// <see cref="MutationOptionMapper"/> and keeps part-id -> icon on the side for the view.
    /// </summary>
    public class MutationOptionCatalog : IMutationOptionCatalog
    {
        private readonly Dictionary<string, IReadOnlyList<MutationOption>> _byArchetype;
        private readonly Dictionary<string, Sprite> _iconByPart;

        public MutationOptionCatalog(IReadOnlyList<ArchetypePartSetDefinition> sets)
        {
            if (sets == null)
            {
                throw new ArgumentNullException(nameof(sets));
            }

            _byArchetype = new Dictionary<string, IReadOnlyList<MutationOption>>(sets.Count, StringComparer.Ordinal);
            _iconByPart = new Dictionary<string, Sprite>(StringComparer.Ordinal);

            foreach (var set in sets)
            {
                if (set == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(set.ArchetypeId))
                {
                    throw new InvalidOperationException(
                        $"[MutationOptionCatalog] Part-set asset '{set.name}' has an empty archetype id.");
                }

                if (_byArchetype.ContainsKey(set.ArchetypeId))
                {
                    throw new InvalidOperationException(
                        $"[MutationOptionCatalog] Duplicate archetype id '{set.ArchetypeId}' " +
                        $"(asset '{set.name}'). One part-set asset per archetype.");
                }

                _byArchetype.Add(set.ArchetypeId, MutationOptionMapper.ToOptions(set));

                foreach (var entry in set.Options)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.PartId) || entry.Icon == null)
                    {
                        continue;
                    }

                    // First authored icon for a part wins; later duplicates are ignored.
                    if (!_iconByPart.ContainsKey(entry.PartId))
                    {
                        _iconByPart.Add(entry.PartId, entry.Icon);
                    }
                }
            }
        }

        public IReadOnlyList<MutationOption> OptionsFor(string archetypeId)
        {
            if (!string.IsNullOrEmpty(archetypeId) && _byArchetype.TryGetValue(archetypeId, out var options))
            {
                return options;
            }

            return Array.Empty<MutationOption>();
        }

        public bool TryGetIcon(string partId, out Sprite icon)
        {
            if (string.IsNullOrEmpty(partId))
            {
                icon = null;
                return false;
            }

            return _iconByPart.TryGetValue(partId, out icon);
        }
    }
}
