using System;
using System.Collections.Generic;
using Mutation.Data.Definitions;

namespace Mutation.Data
{
    /// <summary>
    /// Dictionary-backed archetype lookup built once from the authored definitions.
    /// Fails fast on duplicate or empty ids so authoring mistakes surface at startup.
    /// Mirrors <c>Inventory.Data.ArtifactCatalog</c>.
    /// </summary>
    public class ArchetypeCatalog : IArchetypeCatalog
    {
        private readonly List<ArchetypeDefinition> _all;
        private readonly Dictionary<string, ArchetypeDefinition> _byId;

        public IReadOnlyList<ArchetypeDefinition> All => _all;

        public ArchetypeCatalog(IReadOnlyList<ArchetypeDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<ArchetypeDefinition>(definitions.Count);
            _byId = new Dictionary<string, ArchetypeDefinition>(definitions.Count);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[ArchetypeCatalog] Archetype definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[ArchetypeCatalog] Duplicate archetype id '{definition.Id}' (asset '{definition.name}').");
                }

                _all.Add(definition);
                _byId.Add(definition.Id, definition);
            }
        }

        public bool Contains(string archetypeId)
        {
            return !string.IsNullOrEmpty(archetypeId) && _byId.ContainsKey(archetypeId);
        }

        public bool TryGet(string archetypeId, out ArchetypeDefinition definition)
        {
            if (string.IsNullOrEmpty(archetypeId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(archetypeId, out definition);
        }
    }
}
