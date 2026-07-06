using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The draft's authored tunables as Core sees them (mapped from the ArenaDraftConfig SO at
    /// install time; tests construct them directly). Timer semantics: humans get the generous
    /// soft limit, AI seats the short pacing delay, departed seats fill immediately.
    /// </summary>
    public class ArenaDraftSettings
    {
        public IReadOnlyList<string> SlotLoadout { get; }
        public IReadOnlyList<ArenaDraftPartInfo> FloorParts { get; }
        public int CatalogSampleSize { get; }
        public float PickTimerSeconds { get; }
        public float AiPickDelaySeconds { get; }

        public ArenaDraftSettings(
            IReadOnlyList<string> slotLoadout,
            IReadOnlyList<ArenaDraftPartInfo> floorParts,
            int catalogSampleSize,
            float pickTimerSeconds,
            float aiPickDelaySeconds)
        {
            SlotLoadout = slotLoadout ?? Array.Empty<string>();
            FloorParts = floorParts ?? Array.Empty<ArenaDraftPartInfo>();
            CatalogSampleSize = catalogSampleSize;
            PickTimerSeconds = pickTimerSeconds;
            AiPickDelaySeconds = aiPickDelaySeconds;
        }
    }
}
