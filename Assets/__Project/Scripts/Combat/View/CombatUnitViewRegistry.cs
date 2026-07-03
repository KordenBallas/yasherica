using System.Collections.Generic;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Plain dictionary implementation of the unit-visual registry.
    /// </summary>
    public class CombatUnitViewRegistry : ICombatUnitViewRegistry
    {
        private readonly Dictionary<int, Transform> _visualRoots = new Dictionary<int, Transform>();

        public void Register(int unitId, Transform visualRoot)
        {
            _visualRoots[unitId] = visualRoot;
        }

        public bool TryGet(int unitId, out Transform visualRoot)
        {
            if (_visualRoots.TryGetValue(unitId, out visualRoot) && visualRoot != null)
                return true;

            visualRoot = null;
            return false;
        }

        public void Clear()
        {
            _visualRoots.Clear();
        }
    }
}
