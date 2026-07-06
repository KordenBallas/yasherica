using UnityEngine;

namespace Hub.Data
{
    /// <summary>
    /// Race id → belonging colour for card tints (O1). Lives in the Data layer because the
    /// belonging colour is authored on the <c>RaceDefinition</c> SO (Core race records hold no
    /// Unity types). Unknown/kindless ids read as white.
    /// </summary>
    public interface IRaceTintCatalog
    {
        Color TintFor(string raceId);
    }
}
