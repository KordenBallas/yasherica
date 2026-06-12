using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Pure runtime representation of a crafting recipe:
    /// an unordered multiset of input definition ids producing one output definition id.
    /// </summary>
    public sealed class RecipeData
    {
        public IReadOnlyList<string> InputDefinitionIds { get; }
        public string OutputDefinitionId { get; }

        public RecipeData(IReadOnlyList<string> inputDefinitionIds, string outputDefinitionId)
        {
            InputDefinitionIds = inputDefinitionIds;
            OutputDefinitionId = outputDefinitionId;
        }
    }
}
