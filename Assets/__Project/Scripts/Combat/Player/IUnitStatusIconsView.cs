using System.Collections.Generic;

namespace Combat.Player
{
    /// <summary>
    /// View seam for the per-unit on-board status row (icon + remaining turns per active
    /// status). Implemented by a thin world-space MonoBehaviour; the presenter decides
    /// what each row shows.
    /// </summary>
    public interface IUnitStatusIconsView
    {
        void SetIcons(int unitId, IReadOnlyList<StatusIconModel> icons);
        void ClearAll();
    }
}
