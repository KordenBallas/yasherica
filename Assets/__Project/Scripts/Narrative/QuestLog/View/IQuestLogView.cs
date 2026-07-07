using System;
using Narrative.QuestLog.Core;

namespace Narrative.QuestLog.View
{
    /// <summary>
    /// Adapter contract for the quest log / saga readout panel (P1-11): a read-only window the
    /// player toggles open; rendering a model mutates nothing.
    /// </summary>
    public interface IQuestLogView
    {
        /// <summary>Raised when the player presses the toggle key.</summary>
        event Action OnToggleRequested;

        bool IsVisible { get; }

        /// <summary>Renders the model and shows the panel.</summary>
        void Show(QuestLogModel model);

        void Hide();
    }
}
