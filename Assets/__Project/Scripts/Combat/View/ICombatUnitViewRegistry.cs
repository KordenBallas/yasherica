using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Maps combat unit ids to their scene visual roots so presentation (overhead icons,
    /// ghost clones) can find the GameObject of a domain unit. Populated by the unit
    /// initializers, cleared when combat ends.
    /// </summary>
    public interface ICombatUnitViewRegistry
    {
        void Register(int unitId, Transform visualRoot);
        bool TryGet(int unitId, out Transform visualRoot);
        void Clear();
    }
}
