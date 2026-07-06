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
        /// <summary>Rough footprint radii by kind (world units at scale 1) — the light-authoring
        /// defaults of the decoration-footprint brief FR2; a kit entry may override.</summary>
        public const float SmallFootprintDefault = 0.35f;
        public const float LargeFootprintDefault = 0.9f;
        public const float BlockingFootprintDefault = 1.0f;

        public FeatureEntryData(
            FeatureKind kind,
            int weight,
            float scaleMin,
            float scaleMax,
            float footprintRadius = -1f,
            bool mayOverhang = false)
        {
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "Feature weight must be >= 0.");
            }

            Kind = kind;
            Weight = weight;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
            FootprintRadius = footprintRadius > 0f ? footprintRadius : DefaultFootprint(kind);
            MayOverhang = mayOverhang;
        }

        public FeatureKind Kind { get; }

        /// <summary>Relative draw weight among entries of the same kind; 0 = never drawn.</summary>
        public int Weight { get; }

        public float ScaleMin { get; }

        public float ScaleMax { get; }

        /// <summary>Rough horizontal radius (world units at scale 1) — the keep-clear margin the
        /// prop needs from the platform edge (decoration-footprint brief FR1).</summary>
        public float FootprintRadius { get; }

        /// <summary>Opt-in framing style (brief FR5): only a flagged prop may anchor on the rim
        /// and lean past the walkable edge; every other prop obeys the footprint margin.</summary>
        public bool MayOverhang { get; }

        public static float DefaultFootprint(FeatureKind kind)
        {
            switch (kind)
            {
                case FeatureKind.LargeDecorative: return LargeFootprintDefault;
                case FeatureKind.Blocking: return BlockingFootprintDefault;
                default: return SmallFootprintDefault;
            }
        }
    }
}
