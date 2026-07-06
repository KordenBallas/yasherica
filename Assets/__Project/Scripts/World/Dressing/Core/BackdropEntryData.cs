using System;

namespace World.Dressing.Core
{
    /// <summary>
    /// One distant-scatter entry of a backdrop kit, mapped from the kit asset (the kit's prefab
    /// list is index-aligned with these — the planner picks by entry index, the spawner resolves
    /// the prefab). Immutable; pure C#.
    /// </summary>
    public sealed class BackdropEntryData
    {
        public BackdropEntryData(int weight, float scaleMin, float scaleMax)
        {
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "Backdrop weight must be >= 0.");
            }

            Weight = weight;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
        }

        /// <summary>Relative draw weight; 0 = never drawn (a broken entry keeps its slot).</summary>
        public int Weight { get; }

        public float ScaleMin { get; }

        public float ScaleMax { get; }
    }
}
