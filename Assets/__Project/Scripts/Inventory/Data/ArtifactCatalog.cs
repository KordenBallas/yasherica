using System;
using System.Collections.Generic;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Dictionary-backed artifact lookup built once from the authored definitions.
    /// Fails fast on duplicate or empty ids so authoring mistakes surface at startup.
    /// </summary>
    public class ArtifactCatalog : IArtifactCatalog
    {
        private readonly List<ArtifactDefinition> _all;
        private readonly Dictionary<string, ArtifactDefinition> _byId;

        public IReadOnlyList<ArtifactDefinition> All => _all;

        public ArtifactCatalog(IReadOnlyList<ArtifactDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<ArtifactDefinition>(definitions.Count);
            _byId = new Dictionary<string, ArtifactDefinition>(definitions.Count);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[ArtifactCatalog] Artifact definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[ArtifactCatalog] Duplicate artifact id '{definition.Id}' (asset '{definition.name}').");
                }

                _all.Add(definition);
                _byId.Add(definition.Id, definition);
            }
        }

        public bool TryGet(string definitionId, out ArtifactDefinition definition)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(definitionId, out definition);
        }
    }
}
