using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Data-layer view of the authored Part-Blank pool: the pure records (via
    /// <see cref="IPartBlankDataSource"/>) plus the icon lookup the presenter
    /// resolves at the view boundary, so Core never holds Unity types.
    /// </summary>
    public interface IPartBlankCatalog : IPartBlankDataSource
    {
        bool TryGetIcon(string definitionId, out Sprite icon);
    }
}
