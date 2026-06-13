using System;
using System.Collections.Generic;
using CharacterSystem.Data.Definitions;

namespace CharacterSystem.Data
{
    /// <summary>
    /// Dictionary-backed attachment lookup built once from the authored definitions.
    /// Fails fast on duplicate or empty ids so authoring mistakes surface at startup.
    /// </summary>
    public class AttachmentCatalog : IAttachmentCatalog
    {
        private readonly List<AttachmentDefinition> _all;
        private readonly Dictionary<string, AttachmentDefinition> _byId;

        public IReadOnlyList<AttachmentDefinition> All => _all;

        public AttachmentCatalog(IReadOnlyList<AttachmentDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _all = new List<AttachmentDefinition>(definitions.Count);
            _byId = new Dictionary<string, AttachmentDefinition>(definitions.Count);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[AttachmentCatalog] Attachment definition '{definition.name}' has an empty id.");
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException(
                        $"[AttachmentCatalog] Duplicate attachment id '{definition.Id}' (asset '{definition.name}').");
                }

                _all.Add(definition);
                _byId.Add(definition.Id, definition);
            }
        }

        public bool TryGet(string attachmentId, out AttachmentDefinition definition)
        {
            if (string.IsNullOrEmpty(attachmentId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(attachmentId, out definition);
        }
    }
}
