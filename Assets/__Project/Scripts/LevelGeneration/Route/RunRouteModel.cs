using System;
using System.Collections.Generic;
using Loot.Core;
using Narrative.Director.Core;

namespace LevelGeneration.Route
{
    /// <summary>
    /// The routed-path model (world-backdrop-and-elevation brief, FR A–B): a pure function of
    /// forward distance that answers, for any layout-cursor position, where the trail sits laterally
    /// and on which elevation tier. Lateral = a bounded low-frequency baseline wander (two seeded
    /// incommensurate sinusoids — a trail with inertia, never a zigzag) plus sparse feature arcs
    /// that bulge toward the camera around a far-side landmark. Everything derives from the route
    /// seed with slot-local arc draws, so sampling needs no lookahead and no cross-window state —
    /// streaming and same-seed replay are structural.
    /// </summary>
    public sealed class RunRouteModel
    {
        /// <summary>
        /// Which lateral sign faces the fixed isometric camera. The rig looks from the -Z/-X side,
        /// so arcs bulge toward -Z; flip only here if a camera pass ever proves otherwise.
        /// </summary>
        public const float TowardCameraSign = -1f;

        private const string PhaseSeedContext = "route-phase";
        private const string ArcSeedContext = "route-arc";

        /// <summary>Integer resolution of seeded phase/unit draws (exact-integer arithmetic).</summary>
        private const int PhaseSteps = 1024;
        private const int UnitSteps = 10000;

        // Two incommensurate harmonics per axis: the irrational-ish wavelength ratios keep the sum
        // from ever repeating as a visible pattern while staying bounded by the weight sum.
        private const float SecondaryBaselineWavelengthRatio = 2.6f;
        private const float PrimaryBaselineWeight = 0.7f;
        private const float SecondaryBaselineWeight = 0.3f;
        private const float SecondaryTierWavelengthRatio = 0.37f;
        private const float PrimaryTierWeight = 0.4f;
        private const float SecondaryTierWeight = 0.1f;

        // Apex window [0.3, 0.7] of the slot pairs with the half-length cap
        // (BiomeLandscapeSettings.MaxArcHalfLengthSlotFraction) so an arc's support never leaves
        // its slot — Sample(x) only ever consults the slot containing x.
        private const float ApexWindowStartFraction = BiomeLandscapeSettings.MaxArcHalfLengthSlotFraction;
        private const float ApexWindowFraction = 0.4f;

        /// <summary>Weakest arc as a fraction of the biome's arc depth (avoids unreadably shallow bends).</summary>
        private const float MinArcDepthFraction = 0.6f;

        /// <summary>
        /// Fixed landmark base height, world units, slightly below tier 0. Deliberately NOT coupled
        /// to the local tier: on the tilted orthographic camera a +Z offset already shifts a
        /// landmark up-screen, and adding tier height on top pushes it out of the frame; a low
        /// constant base keeps the landform's flank in the visible top band (its underside is
        /// hidden behind the platforms anyway — the world below is void).
        /// </summary>
        private const float LandmarkBaseY = -1.5f;

        private const int KitIndexRange = 1024;
        private const float TwoPi = 2f * (float)Math.PI;

        private readonly BiomeLandscapeSettings _settings;
        private readonly int _routeSeed;
        private readonly float _baselinePhase1;
        private readonly float _baselinePhase2;
        private readonly float _tierPhase1;
        private readonly float _tierPhase2;

        public RunRouteModel(BiomeLandscapeSettings settings, int routeSeed)
        {
            _settings = settings ?? BiomeLandscapeSettings.CreateDefault();
            _routeSeed = routeSeed;

            var rng = CreateStream(PhaseSeedContext);
            _baselinePhase1 = NextPhase(rng);
            _baselinePhase2 = NextPhase(rng);
            _tierPhase1 = NextPhase(rng);
            _tierPhase2 = NextPhase(rng);
        }

        public BiomeLandscapeSettings Settings => _settings;

        /// <summary>The route at one forward position. Pure and deterministic per (seed, settings).</summary>
        public RouteSample Sample(float forwardX)
        {
            float lateralZ = BaselineZ(forwardX) + ArcZ(forwardX);
            int tierIndex = TierIndexAt(forwardX);
            return new RouteSample(lateralZ, tierIndex * _settings.TierStep, tierIndex);
        }

        /// <summary>
        /// Landmarks whose arc apex lies in the half-open window [fromX, toXExclusive). Contiguous
        /// windows therefore emit every landmark exactly once — the streaming scan hook.
        /// </summary>
        public IReadOnlyList<LandmarkSpec> GetLandmarksInRange(float fromX, float toXExclusive)
        {
            var result = new List<LandmarkSpec>();
            if (toXExclusive <= fromX)
            {
                return result;
            }

            float slotLength = _settings.ArcSlotLength;
            int firstSlot = Math.Max(0, (int)Math.Floor(fromX / slotLength));
            int lastSlot = Math.Max(0, (int)Math.Floor(toXExclusive / slotLength));
            for (int slot = firstSlot; slot <= lastSlot; slot++)
            {
                var arc = ArcFor(slot);
                if (!arc.Occurs || arc.ApexX < fromX || arc.ApexX >= toXExclusive)
                {
                    continue;
                }

                float z = BaselineZ(arc.ApexX) + _settings.LandmarkOffset;
                result.Add(new LandmarkSpec(arc.ApexX, z, LandmarkBaseY, arc.KitIndex, arc.Scale));
            }

            return result;
        }

        private float BaselineZ(float x)
        {
            float wavelength = _settings.BaselineWavelength;
            float primary = (float)Math.Sin(TwoPi * x / wavelength + _baselinePhase1);
            float secondary = (float)Math.Sin(
                TwoPi * x / (wavelength * SecondaryBaselineWavelengthRatio) + _baselinePhase2);
            return _settings.BaselineAmplitude
                   * (PrimaryBaselineWeight * primary + SecondaryBaselineWeight * secondary);
        }

        private float ArcZ(float x)
        {
            int slot = (int)Math.Floor(x / _settings.ArcSlotLength);
            if (slot < 0)
            {
                return 0f;
            }

            var arc = ArcFor(slot);
            if (!arc.Occurs)
            {
                return 0f;
            }

            float distance = Math.Abs(x - arc.ApexX);
            if (distance >= arc.HalfLength)
            {
                return 0f;
            }

            // cos² bump: C1-smooth, zero at both edges, full depth at the apex.
            float wave = (float)Math.Cos(Math.PI * distance / (2f * arc.HalfLength));
            return TowardCameraSign * arc.Depth * wave * wave;
        }

        private int TierIndexAt(float x)
        {
            float wavelength = _settings.TierWavelength;
            float primary = (float)Math.Sin(TwoPi * x / wavelength + _tierPhase1);
            float secondary = (float)Math.Sin(
                TwoPi * x / (wavelength * SecondaryTierWavelengthRatio) + _tierPhase2);
            float normalized = 0.5f + PrimaryTierWeight * primary + SecondaryTierWeight * secondary;
            normalized = normalized < 0f ? 0f : normalized > 1f ? 1f : normalized;
            return (int)Math.Round(normalized * (_settings.TierCount - 1));
        }

        private readonly struct ArcInfo
        {
            public readonly bool Occurs;
            public readonly float ApexX;
            public readonly float HalfLength;
            public readonly float Depth;
            public readonly int KitIndex;
            public readonly float Scale;

            public ArcInfo(bool occurs, float apexX, float halfLength, float depth, int kitIndex, float scale)
            {
                Occurs = occurs;
                ApexX = apexX;
                HalfLength = halfLength;
                Depth = depth;
                KitIndex = kitIndex;
                Scale = scale;
            }
        }

        private ArcInfo ArcFor(int slot)
        {
            // One private stream per slot; the draw order below is the arc's wire format — reorder
            // it and every seed's layout changes.
            var rng = CreateStream($"{ArcSeedContext}:{slot}");
            if (rng.NextInt(100) >= _settings.ArcChancePercent)
            {
                return default;
            }

            float slotStart = slot * _settings.ArcSlotLength;
            float apexX = slotStart + _settings.ArcSlotLength
                * (ApexWindowStartFraction + ApexWindowFraction * NextUnit(rng));
            float halfLength = _settings.ArcHalfLengthMin
                + (_settings.ArcHalfLengthMax - _settings.ArcHalfLengthMin) * NextUnit(rng);
            float depth = _settings.ArcDepth
                * (MinArcDepthFraction + (1f - MinArcDepthFraction) * NextUnit(rng));
            int kitIndex = rng.NextInt(KitIndexRange);
            float scale = _settings.LandmarkScaleMin
                + (_settings.LandmarkScaleMax - _settings.LandmarkScaleMin) * NextUnit(rng);

            return new ArcInfo(true, apexX, halfLength, depth, kitIndex, scale);
        }

        private IRandomSource CreateStream(string context)
        {
            int seed = LootSeed.Derive(_routeSeed, context);
            return new DeterministicRandom(unchecked((ulong)seed));
        }

        private static float NextPhase(IRandomSource rng)
        {
            return rng.NextInt(PhaseSteps) / (float)PhaseSteps * TwoPi;
        }

        private static float NextUnit(IRandomSource rng)
        {
            return rng.NextInt(UnitSteps + 1) / (float)UnitSteps;
        }
    }
}
