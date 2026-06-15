using System;
using System.Collections.Generic;

namespace Inventory.View
{
    /// <summary>
    /// Adapter contract for the feeding UI: the artifacts staged in the feeding tray, the Feed
    /// confirm control, and the readout panel (cumulative archetype weights of the current
    /// selection, this-stage digestion progress, and the dominant archetype(s)).
    /// Shown only while the inventory is in feeding mode.
    /// </summary>
    public interface IFeedingView
    {
        /// <summary>Raised when the player clicks an artifact in the feeding tray (to unselect it).</summary>
        event Action<int> OnTrayItemClicked;

        /// <summary>Raised when the player confirms feeding the tray.</summary>
        event Action OnFeedClicked;

        /// <summary>Rebuilds the tray artifact views.</summary>
        void ShowTray(IReadOnlyList<ArtifactViewData> items);

        /// <summary>Enables/disables the Feed control (disabled when the tray is empty).</summary>
        void SetFeedEnabled(bool enabled);

        /// <summary>Cumulative archetype weights of everything currently in the tray.</summary>
        void SetCumulativeReadout(IReadOnlyList<ArchetypeReadoutEntry> entries);

        /// <summary>This-stage digestion progress toward the mutate-ready threshold.</summary>
        void SetProgression(int fed, int threshold, float normalized);

        /// <summary>The dominant archetype(s) accumulated this stage.</summary>
        void SetDominant(IReadOnlyList<ArchetypeReadoutEntry> dominant);

        /// <summary>Shows or hides the whole feeding UI (feeding mode on/off).</summary>
        void SetVisible(bool visible);
    }
}
