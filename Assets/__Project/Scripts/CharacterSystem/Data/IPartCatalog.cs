using System.Collections.Generic;
using CharacterSystem.Data.Definitions;

namespace CharacterSystem.Data
{
    public interface IPartCatalog
    {
        IReadOnlyList<PartDefinition> All { get; }
        bool TryGet(string partId, out PartDefinition definition);
    }
}
