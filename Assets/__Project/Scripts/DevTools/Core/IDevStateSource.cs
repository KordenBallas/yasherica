using System.Collections.Generic;

namespace DevTools.Core
{
    /// <summary>
    /// Builds the current developer-overlay sections from live game state. Pure C# and UnityEngine-free
    /// so the section content is unit-testable; the overlay view renders whatever this returns.
    /// </summary>
    public interface IDevStateSource
    {
        /// <summary>Snapshots the live state into display sections (called on demand by the view).</summary>
        IReadOnlyList<DevPanelSection> BuildSections();
    }
}
