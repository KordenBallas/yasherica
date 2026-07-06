using System;

namespace World.Dressing.Core
{
    /// <summary>
    /// One feature-pool entry of a biome-feature kit, mapped from the kit asset (the kit's prefab
    /// list is index-aligned with these — the planner picks by entry index, the spawner resolves
    /// the prefab). Immutable; pure C#.
    /// </summary>
    public sealed class FeatureEntryData
    {
        public FeatureEntryData(FeatureKind kind, int weight, float scaleMin, float scaleMax)
        {
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "Feature weight must be >= 0.");
            }

            Kind = kind;
            Weight = weight;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
        }

        public FeatureKind Kind { get; }

        /// <summary>Relative draw weight among entries of the same kind; 0 = never drawn.</summary>
        public int Weight { get; }

        public float ScaleMin { get; }

        public float ScaleMax { get; }
    }
}
