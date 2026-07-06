using System;
using System.Collections.Generic;

namespace Core.Persistence
{
    /// <summary>
    /// The creature the player built (FR4): governing skeleton/frame plus the equipped and
    /// equipped-but-dormant parts, as parallel slotId/partId lists (JsonUtility-friendly). Part and
    /// skeleton content resolves by id from the catalogs at restore time.
    /// </summary>
    [Serializable]
    public class HeroBodySnapshot
    {
        public string SkeletonId = string.Empty;
        public List<string> EquippedSlotIds = new List<string>();
        public List<string> EquippedPartIds = new List<string>();
        public List<string> DormantSlotIds = new List<string>();
        public List<string> DormantPartIds = new List<string>();
    }
}
