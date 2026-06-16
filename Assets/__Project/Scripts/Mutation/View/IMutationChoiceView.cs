using System;
using System.Collections.Generic;

namespace Mutation.View
{
    /// <summary>
    /// Adapter contract for the stage-up mutation choice panel: shows the offered mutations and
    /// forwards the player's pick by index. Hidden until the character is ready to mutate.
    /// </summary>
    public interface IMutationChoiceView
    {
        /// <summary>Raised when the player picks the option at the given index.</summary>
        event Action<int> OnChoiceSelected;

        /// <summary>Rebuilds the choice buttons from the offered options (in order).</summary>
        void ShowChoices(IReadOnlyList<MutationChoiceViewData> options);

        /// <summary>Shows or hides the whole choice panel.</summary>
        void SetVisible(bool visible);
    }
}
