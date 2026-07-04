using System;
using Loot.Core;
using Narrative.Director.Core;

namespace LevelGeneration.Route
{
    /// <summary>
    /// Pure ridge-line generator for the distant world backdrop (world-backdrop-and-elevation brief,
    /// FR C): per layer, a bounded undulating height profile from two seeded incommensurate
    /// sinusoids plus a small per-sample jitter, flavored by the biome's ridge amplitude/wavelength.
    /// Deterministic per (seed, layer) — same run, same horizon.
    /// </summary>
    public sealed class BackdropSilhouetteModel
    {
        private const string LayerSeedContext = "backdrop-layer";

        private const int PhaseSteps = 1024;
        private const int UnitSteps = 10000;
        private const float SecondaryWavelengthRatio = 0.41f;
        private const float PrimaryWeight = 0.3f;
        private const float SecondaryWeight = 0.15f;
        private const float JitterWeight = 0.05f;
        private const float TwoPi = 2f * (float)Math.PI;

        private readonly BiomeLandscapeSettings _settings;
        private readonly int _seed;

        public BackdropSilhouetteModel(BiomeLandscapeSettings settings, int seed)
        {
            _settings = settings ?? BiomeLandscapeSettings.CreateDefault();
            _seed = seed;
        }

        /// <summary>
        /// Ridge heights for one layer, evenly sampled across <paramref name="width"/>. Every value
        /// lies in [0, BackdropRidgeAmplitude].
        /// </summary>
        public float[] GetRidgeHeights(int layerIndex, int sampleCount, float width)
        {
            if (sampleCount < 2)
            {
                sampleCount = 2;
            }

            int layerSeed = LootSeed.Derive(_seed, $"{LayerSeedContext}:{layerIndex}");
            var rng = new DeterministicRandom(unchecked((ulong)layerSeed));
            float phase1 = rng.NextInt(PhaseSteps) / (float)PhaseSteps * TwoPi;
            float phase2 = rng.NextInt(PhaseSteps) / (float)PhaseSteps * TwoPi;

            float amplitude = _settings.BackdropRidgeAmplitude;
            float wavelength = _settings.BackdropRidgeWavelength;

            var heights = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float x = i / (float)(sampleCount - 1) * width;
                float jitter = (rng.NextInt(UnitSteps + 1) / (float)UnitSteps - 0.5f) * 2f;
                float normalized = 0.5f
                    + PrimaryWeight * (float)Math.Sin(TwoPi * x / wavelength + phase1)
                    + SecondaryWeight * (float)Math.Sin(TwoPi * x / (wavelength * SecondaryWavelengthRatio) + phase2)
                    + JitterWeight * jitter;
                normalized = normalized < 0f ? 0f : normalized > 1f ? 1f : normalized;
                heights[i] = normalized * amplitude;
            }

            return heights;
        }
    }
}
