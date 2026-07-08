using System;

namespace Combat.Arena.View
{
    /// <summary>
    /// View contract for the in-match HUD: the status line (round / waiting / winner), the
    /// spectating label for a defeated local player, a desync warning, and the Leave button.
    /// </summary>
    public interface IArenaMatchHudView
    {
        event Action LeaveClicked;

        void SetStatus(string message);
        void SetSpectatingVisible(bool visible);
        void SetLeaveVisible(bool visible);
        void ShowDesyncWarning();

        /// <summary>Transient seat-liveness line ("Player 3 disconnected — auto-passing"); null/empty hides it.</summary>
        void SetSeatNotice(string message);
    }
}
