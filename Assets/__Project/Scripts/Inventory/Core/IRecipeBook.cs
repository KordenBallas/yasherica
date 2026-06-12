using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Lookup of crafting recipes. Matching is order-independent and multiset-exact:
    /// the provided inputs must equal a recipe's inputs including duplicate counts.
    /// </summary>
    public interface IRecipeBook
    {
        bool TryMatch(IReadOnlyList<string> inputDefinitionIds, out string outputDefinitionId);
    }
}
