using System;

namespace MainMenu
{
    /// <summary>
    /// View contract for the boot menu: mode buttons plus the Continue entry point (P2-2 A1 —
    /// shown only while an in-progress run save exists). The view only surfaces clicks and
    /// visibility; all routing lives in <see cref="MainMenuPresenter"/>.
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>Raised when the player picks Journey (a NEW single-player run).</summary>
        event Action JourneyClicked;

        /// <summary>Raised when the player picks Continue (resume the in-progress run).</summary>
        event Action ContinueClicked;

        /// <summary>Raised when the player picks Arena (the networked FFA mode).</summary>
        event Action ArenaClicked;

        /// <summary>Shows/hides the Continue entry point (visible iff a run save exists).</summary>
        void SetContinueVisible(bool visible);
    }
}
