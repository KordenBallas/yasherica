using System.Collections.Generic;
using CharacterSystem.Data.Definitions;

namespace CharacterSystem.Data
{
    public interface IAttachmentCatalog
    {
        IReadOnlyList<AttachmentDefinition> All { get; }
        bool TryGet(string attachmentId, out AttachmentDefinition definition);
    }
}
