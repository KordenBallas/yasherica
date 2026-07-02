using System;
using System.Collections.Generic;
using Mutation.Core;
using Mutation.Data.Definitions;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Builds the pure Part-Blank records from the authored PartBlankDefinition
    /// assets (the only bridge from the blank Data layer into Core). Fails fast on
    /// duplicate or empty ids; a missing slot reference yields a null slot id that
    /// the content validator reports.
    /// </summary>
    public class PartBlankCatalog : IPartBlankCatalog
    {
        private readonly List<PartBlankData> _all;
        private readonly Dictionary<string, PartBlankData> _byId;
        private readonly Dictionary<string, Sprite> _iconById;

        public IReadOnlyList<PartBlankData> All => _all;

        public PartBlankCatalog(IReadOnlyList<PartBlankDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<PartBlankData>(definitions.Count);
            _byId = new Dictionary<string, PartBlankData>(definitions.Count, StringComparer.Ordinal);
            _iconById = new Dictionary<string, Sprite>(definitions.Count, StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[PartBlankCatalog] Blank definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[PartBlankCatalog] Duplicate blank id '{definition.Id}' (asset '{definition.name}').");
                }

                var displayName = string.IsNullOrEmpty(definition.DisplayName)
                    ? definition.name
                    : definition.DisplayName;
                var blank = new PartBlankData(
                    definition.Id,
                    displayName,
                    definition.Slot != null ? definition.Slot.Id : null,
                    definition.SpeciesArchetypeId,
                    definition.SocketCount);

                _all.Add(blank);
                _byId.Add(definition.Id, blank);

                if (definition.Icon != null)
                {
                    _iconById.Add(definition.Id, definition.Icon);
                }
            }
        }

        public bool TryGet(string definitionId, out PartBlankData blank)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                blank = null;
                return false;
            }

            return _byId.TryGetValue(definitionId, out blank);
        }

        public bool TryGetIcon(string definitionId, out Sprite icon)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                icon = null;
                return false;
            }

            return _iconById.TryGetValue(definitionId, out icon);
        }
    }
}
