namespace LevelGeneration.Route
{
    /// <summary>
    /// Immutable, UnityEngine-free per-biome landscape-character dials (the world-backdrop-and-
    /// elevation brief): the routed path's bounded corridor and curve character, the sparse
    /// feature-arc dials with their justifying midground landmarks, the elevation tier set, and the
    /// distant backdrop ridge character. Mapped from the <c>BiomeAppearanceDefinition</c> SO at
    /// install time; <see cref="CreateDefault"/> is the fallback for an unauthored biome.
    /// </summary>
    public sealed class BiomeLandscapeSettings
    {
        /// <summary>
        /// Upper bound on an arc half-length as a fraction of the slot, paired with the model's apex
        /// window so an arc's support can never leave its own slot (keeps sampling slot-local).
        /// </summary>
        public const float MaxArcHalfLengthSlotFraction = 0.3f;

        /// <summary>
        /// Extra depth clearance a routing landmark keeps beyond the worst-case local path swing
        /// (2 × baseline amplitude), so it always reads as sitting behind the trail it justifies.
        /// </summary>
        public const float MinLandmarkClearance = 4f;

        /// <summary>Max lateral (depth) excursion of the whole route from the run axis, world units.</summary>
        public float CorridorHalfWidth { get; }

        /// <summary>Forward distance of one baseline bend, world units (several platforms per bend).</summary>
        public float BaselineWavelength { get; }

        /// <summary>Lateral amplitude of the baseline wander (clamped so arcs still fit the corridor).</summary>
        public float BaselineAmplitude { get; }

        /// <summary>Forward length of one feature-arc slot; at most one arc occurs per slot.</summary>
        public float ArcSlotLength { get; }

        /// <summary>Chance (0–100) that a slot carries a feature arc.</summary>
        public int ArcChancePercent { get; }

        /// <summary>Max extra toward-camera bulge of a feature arc, world units.</summary>
        public float ArcDepth { get; }

        /// <summary>Inclusive range of an arc's half-length along the run, world units.</summary>
        public float ArcHalfLengthMin { get; }
        public float ArcHalfLengthMax { get; }

        /// <summary>How far behind the baseline (away from the camera) an arc's landmark sits.</summary>
        public float LandmarkOffset { get; }

        /// <summary>Inclusive uniform scale range for a routing landmark.</summary>
        public float LandmarkScaleMin { get; }
        public float LandmarkScaleMax { get; }

        /// <summary>Number of deliberate elevation levels the field undulates across.</summary>
        public int TierCount { get; }

        /// <summary>Vertical distance between adjacent tiers, world units (visual only — read, not traversal).</summary>
        public float TierStep { get; }

        /// <summary>Forward distance of one full elevation swell, world units.</summary>
        public float TierWavelength { get; }

        /// <summary>Peak height of the distant backdrop ridge line above its base, world units.</summary>
        public float BackdropRidgeAmplitude { get; }

        /// <summary>Forward distance of one backdrop ridge undulation, world units.</summary>
        public float BackdropRidgeWavelength { get; }

        public BiomeLandscapeSettings(
            float corridorHalfWidth,
            float baselineWavelength,
            float baselineAmplitude,
            float arcSlotLength,
            int arcChancePercent,
            float arcDepth,
            float arcHalfLengthMin,
            float arcHalfLengthMax,
            float landmarkOffset,
            float landmarkScaleMin,
            float landmarkScaleMax,
            int tierCount,
            float tierStep,
            float tierWavelength,
            float backdropRidgeAmplitude,
            float backdropRidgeWavelength)
        {
            CorridorHalfWidth = corridorHalfWidth <= 0f ? DefaultCorridorHalfWidth : corridorHalfWidth;
            BaselineWavelength = baselineWavelength <= 0f ? DefaultBaselineWavelength : baselineWavelength;
            ArcSlotLength = arcSlotLength <= 0f ? DefaultArcSlotLength : arcSlotLength;
            ArcChancePercent = arcChancePercent < 0 ? 0 : arcChancePercent > 100 ? 100 : arcChancePercent;
            ArcDepth = arcDepth < 0f ? 0f : arcDepth > CorridorHalfWidth ? CorridorHalfWidth : arcDepth;

            // Corridor invariant: |baseline| + |arc| <= corridor half-width, always.
            float amplitudeCap = CorridorHalfWidth - ArcDepth;
            BaselineAmplitude = baselineAmplitude < 0f ? 0f
                : baselineAmplitude > amplitudeCap ? amplitudeCap : baselineAmplitude;

            float halfLengthCap = ArcSlotLength * MaxArcHalfLengthSlotFraction;
            ArcHalfLengthMax = arcHalfLengthMax < 1f ? 1f
                : arcHalfLengthMax > halfLengthCap ? halfLengthCap : arcHalfLengthMax;
            ArcHalfLengthMin = arcHalfLengthMin < 1f ? 1f
                : arcHalfLengthMin > ArcHalfLengthMax ? ArcHalfLengthMax : arcHalfLengthMin;

            // Far-side invariant: the landmark clears the worst-case local baseline swing, so it can
            // never end up camera-side of the trail it justifies.
            float landmarkFloor = 2f * BaselineAmplitude + MinLandmarkClearance;
            LandmarkOffset = landmarkOffset < landmarkFloor ? landmarkFloor : landmarkOffset;

            LandmarkScaleMin = landmarkScaleMin <= 0f ? DefaultLandmarkScaleMin : landmarkScaleMin;
            LandmarkScaleMax = landmarkScaleMax < LandmarkScaleMin ? LandmarkScaleMin : landmarkScaleMax;

            TierCount = tierCount < 1 ? 1 : tierCount;
            TierStep = tierStep < 0f ? 0f : tierStep;
            TierWavelength = tierWavelength <= 0f ? DefaultTierWavelength : tierWavelength;

            BackdropRidgeAmplitude = backdropRidgeAmplitude < 0f ? 0f : backdropRidgeAmplitude;
            BackdropRidgeWavelength = backdropRidgeWavelength <= 0f
                ? DefaultBackdropRidgeWavelength : backdropRidgeWavelength;
        }

        // Defaults double as the mapper fallback when no biome appearance asset is authored.
        // Scale reference: platforms are ~8–16 world units across and the traversal camera is a
        // tight orthographic window (~25×14 units), so a readable weave/tier must be commensurate
        // with a platform, not a fraction of one.
        // The baseline stays gentle and the feature arcs carry the felt "turns": the landmark
        // no-occlusion floor is 2·amplitude + clearance, and on the tight tilted view a landmark
        // is only in frame at offsets ≲ 14 — a bolder baseline would push its own landmarks out
        // of the picture. Arc depth is not so constrained (arcs bulge toward the camera).
        public const float DefaultCorridorHalfWidth = 14f;
        public const float DefaultBaselineWavelength = 90f;
        public const float DefaultBaselineAmplitude = 5f;
        public const float DefaultArcSlotLength = 140f;
        public const int DefaultArcChancePercent = 60;
        public const float DefaultArcDepth = 7f;
        public const float DefaultArcHalfLengthMin = 25f;
        public const float DefaultArcHalfLengthMax = 42f;
        public const float DefaultLandmarkOffset = 14f;
        public const float DefaultLandmarkScaleMin = 12f;
        public const float DefaultLandmarkScaleMax = 18f;
        public const int DefaultTierCount = 4;
        public const float DefaultTierStep = 2.5f;
        public const float DefaultTierWavelength = 300f;
        public const float DefaultBackdropRidgeAmplitude = 8f;
        public const float DefaultBackdropRidgeWavelength = 60f;

        public static BiomeLandscapeSettings CreateDefault()
        {
            return new BiomeLandscapeSettings(
                DefaultCorridorHalfWidth,
                DefaultBaselineWavelength, DefaultBaselineAmplitude,
                DefaultArcSlotLength, DefaultArcChancePercent, DefaultArcDepth,
                DefaultArcHalfLengthMin, DefaultArcHalfLengthMax,
                DefaultLandmarkOffset, DefaultLandmarkScaleMin, DefaultLandmarkScaleMax,
                DefaultTierCount, DefaultTierStep, DefaultTierWavelength,
                DefaultBackdropRidgeAmplitude, DefaultBackdropRidgeWavelength);
        }
    }
}
