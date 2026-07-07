using System;
using System.Collections.Generic;

namespace Mutation.View
{
    /// <summary>
    /// Adapter contract for the medallion ribbon under the cauldron: renders
    /// the racked Part-Blanks as medallions with their rim gems and reports
    /// drops, socket clicks, and the unseal confirm. No business logic - the
    /// presenters own the socketing and unseal rules.
    /// </summary>
    public interface IBlankRackView
    {
        /// <summary>Raised when a dragged artifact is released onto a blank's socket.</summary>
        event Action<int /*artifactInstanceId*/, int /*blankInstanceId*/> OnArtifactDroppedOnBlank;

        /// <summary>Raised when a filled socket is clicked (the unsocket gesture).</summary>
        event Action<int /*blankInstanceId*/, int /*artifactInstanceId*/> OnFilledSocketClicked;

        /// <summary>Raised when a ready medallion is clicked (the unseal confirm, Track F).</summary>
        event Action<int /*blankInstanceId*/> OnUnsealClicked;

        /// <summary>Rebuilds the ribbon entries.</summary>
        void ShowBlanks(IReadOnlyList<BlankEntryViewData> blanks);
    }
}
