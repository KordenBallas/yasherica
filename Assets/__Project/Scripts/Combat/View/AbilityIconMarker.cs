using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Tags an overhead plan icon so the hover raycast can identify which unit's plan entry
    /// the mouse is over (queued player ability or committed enemy intent) and replay its ghost.
    /// Data holder only.
    /// </summary>
    public class AbilityIconMarker : MonoBehaviour
    {
        public int UnitId { get; private set; }
        public int QueueIndex { get; private set; }
        public bool IsEnemyIntent { get; private set; }

        public void Initialize(int unitId, int queueIndex, bool isEnemyIntent)
        {
            UnitId = unitId;
            QueueIndex = queueIndex;
            IsEnemyIntent = isEnemyIntent;
        }
    }
}
