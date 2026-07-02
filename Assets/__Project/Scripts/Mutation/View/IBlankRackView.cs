using System;
using System.Collections.Generic;

namespace Mutation.View
{
    /// <summary>
    /// Adapter contract for the operating-table rack left of the cauldron: renders
    /// the racked Part-Blanks with their sockets and reports drops and socket
    /// clicks. No business logic - the presenter owns the socketing rules.
    /// </summary>
    public interface IBlankRackView
    {
        /// <summary>Raised when a dragged artifact is released onto a blank's socket.</summary>
        event Action<int /*artifactInstanceId*/, int /*blankInstanceId*/> OnArtifactDroppedOnBlank;

        /// <summary>Raised when a filled socket is clicked (the unsocket gesture).</summary>
        event Action<int /*blankInstanceId*/, int /*artifactInstanceId*/> OnFilledSocketClicked;

        /// <summary>Rebuilds the rack entries.</summary>
        void ShowBlanks(IReadOnlyList<BlankEntryViewData> blanks);
    }
}
