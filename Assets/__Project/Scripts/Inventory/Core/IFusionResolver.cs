using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Two-tier, no-failure combine resolution: an exact signature recipe wins,
    /// otherwise the emergent grammar derives a result from the inputs' traits.
    /// Every combine yields something.
    /// </summary>
    public interface IFusionResolver
    {
        FusionResult Resolve(IReadOnlyList<string> inputDefinitionIds);
    }
}
