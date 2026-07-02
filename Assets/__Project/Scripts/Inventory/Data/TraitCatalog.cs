using System;
using System.Collections.Generic;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Dictionary-backed trait lookup built once from the authored definitions.
    /// Fails fast on duplicate or empty ids so authoring mistakes surface at startup.
    /// </summary>
    public class TraitCatalog : ITraitCatalog
    {
        private readonly List<TraitDefinition> _all;
        private readonly Dictionary<string, TraitDefinition> _byId;

        public IReadOnlyList<TraitDefinition> All => _all;

        public TraitCatalog(IReadOnlyList<TraitDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<TraitDefinition>(definitions.Count);
            _byId = new Dictionary<string, TraitDefinition>(definitions.Count);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[TraitCatalog] Trait definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[TraitCatalog] Duplicate trait id '{definition.Id}' (asset '{definition.name}').");
                }

                _all.Add(definition);
                _byId.Add(definition.Id, definition);
            }
        }

        public bool Contains(string traitId)
        {
            return !string.IsNullOrEmpty(traitId) && _byId.ContainsKey(traitId);
        }

        public bool TryGet(string traitId, out TraitDefinition definition)
        {
            if (string.IsNullOrEmpty(traitId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(traitId, out definition);
        }
    }
}
