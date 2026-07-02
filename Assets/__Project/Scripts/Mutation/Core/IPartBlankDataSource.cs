using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Pure port onto the authored Part-Blank pool. Implemented by the data layer
    /// (the blank catalog); Core consumers resolve socket counts and slot ids
    /// through this without touching Unity types.
    /// </summary>
    public interface IPartBlankDataSource
    {
        IReadOnlyList<PartBlankData> All { get; }

        bool TryGet(string definitionId, out PartBlankData blank);
    }
}
