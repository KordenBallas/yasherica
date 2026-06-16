using Mutation.Core;
using UnityEngine;

namespace Mutation.Data
{
    /// <summary>
    /// Lookup from archetype id to the body-part options it grants (as Core records, via
    /// <see cref="IMutationOptionProvider"/>), plus the authored icon for a chosen part so the
    /// presenter can build view data. Built once from the authored part-set assets.
    /// </summary>
    public interface IMutationOptionCatalog : IMutationOptionProvider
    {
        /// <summary>The icon authored for <paramref name="partId"/>, if any option references it.</summary>
        bool TryGetIcon(string partId, out Sprite icon);
    }
}
