using System.Collections.Generic;

namespace Hub.Core
{
    /// <summary>
    /// Core port for the Hub's starting-part pool (O1). The Data layer implements it over the
    /// tasted-forms catalog; the presenter and selector only ever see candidates.
    /// </summary>
    public interface IStartingPartPoolSource
    {
        IReadOnlyList<StartingPartCandidate> BuildPool();
    }
}
