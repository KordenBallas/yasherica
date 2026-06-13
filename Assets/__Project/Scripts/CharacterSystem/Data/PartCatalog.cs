using System;
using System.Collections.Generic;
using CharacterSystem.Data.Definitions;

namespace CharacterSystem.Data
{
    /// <summary>
    /// Dictionary-backed part lookup built once from the authored definitions.
    /// Fails fast on duplicate or empty ids so authoring mistakes surface at startup.
    /// </summary>
    public class PartCatalog : IPartCatalog
    {
        private readonly List<PartDefinition> _all;
        private readonly Dictionary<string, PartDefinition> _byId;

        public IReadOnlyList<PartDefinition> All => _all;

        public PartCatalog(IReadOnlyList<PartDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<PartDefinition>(definitions.Count);
            _byId = new Dictionary<string, PartDefinition>(definitions.Count);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[PartCatalog] Part definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[PartCatalog] Duplicate part id '{definition.Id}' (asset '{definition.name}').");
                }

                _all.Add(definition);
                _byId.Add(definition.Id, definition);
            }
        }

        public bool TryGet(string partId, out PartDefinition definition)
        {
            if (string.IsNullOrEmpty(partId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(partId, out definition);
        }
    }
}
