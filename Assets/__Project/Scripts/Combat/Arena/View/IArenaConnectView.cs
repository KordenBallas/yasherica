using System;

namespace Combat.Arena.View
{
    /// <summary>
    /// View contract for the pre-match connect panel: Host, Join-by-address, and the host-only
    /// Start button. Pure event surface — flow lives in <see cref="ArenaConnectPresenter"/>.
    /// </summary>
    public interface IArenaConnectView
    {
        event Action HostClicked;
        event Action<string> JoinClicked;
        event Action StartClicked;

        void SetStatus(string message);
        void SetConnectControlsInteractable(bool interactable);
        void SetStartButtonVisible(bool visible);
        void SetStartButtonInteractable(bool interactable);
        void Hide();
    }
}
