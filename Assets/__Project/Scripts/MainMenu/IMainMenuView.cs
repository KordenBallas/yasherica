using System;

namespace MainMenu
{
    /// <summary>
    /// View contract for the boot menu: two mode buttons, no state. The view only surfaces
    /// clicks; all routing lives in <see cref="MainMenuPresenter"/>.
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>Raised when the player picks Journey (the current single-player game).</summary>
        event Action JourneyClicked;

        /// <summary>Raised when the player picks Arena (the networked FFA mode).</summary>
        event Action ArenaClicked;
    }
}
