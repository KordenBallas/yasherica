using System.Collections.Generic;

namespace Narrative.Director.Core
{
    /// <summary>
    /// The director's committed plan for one window: an ordered list of <see cref="PlannedPlatform"/>s
    /// (always <see cref="RunPacingSettings.WindowSize"/> long, padded with empty fillers). Once the
    /// player enters this window it is locked; the next window is planned separately against the facts
    /// the player changed in the meantime (R7).
    /// </summary>
    public sealed class WindowPlan
    {
        public int WindowIndex { get; }
        public IReadOnlyList<PlannedPlatform> Platforms { get; }

        public WindowPlan(int windowIndex, IReadOnlyList<PlannedPlatform> platforms)
        {
            WindowIndex = windowIndex;
            Platforms = platforms ?? System.Array.Empty<PlannedPlatform>();
        }

        /// <summary>Count of platforms whose chosen story is combat-bearing (the combat budget measure).</summary>
        public int CombatCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Platforms.Count; i++)
                {
                    if (Platforms[i].IsCombat)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
