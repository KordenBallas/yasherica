using System.Collections.Generic;

namespace Combat.Player
{
    /// <summary>
    /// View seam for the overhead plan icons. The presenter pushes per-unit icon models;
    /// the view owns all GameObject/billboard/sprite concerns.
    /// </summary>
    public interface IUnitPlanIconsView
    {
        void SetIcons(int unitId, IReadOnlyList<PlanIconModel> icons);
        void ClearAll();
    }
}
