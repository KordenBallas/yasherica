using System;

namespace Inventory.Core
{
    /// <summary>
    /// Deterministic height field for the boiling liquid surface: two crossed
    /// traveling sine waves whose sum stays within the configured amplitude.
    /// Sampled per vertex by the liquid surface view.
    /// </summary>
    public static class LiquidWaveCalculator
    {
        private const float TwoPi = 6.2831853f;

        // Fixed shear/scale/speed offsets keep the two waves from aligning into a
        // single standing wave, which would read as sloshing instead of boiling.
        private const float PrimaryShear = 0.35f;
        private const float SecondaryShear = -0.5f;
        private const float SecondarySpatialScaleFactor = 1.7f;
        private const float SecondarySpeedFactor = 0.8f;

        public static float SampleHeight(float x, float z, float time, in LiquidWaveSettings settings)
        {
            if (settings.Amplitude <= 0f)
            {
                return 0f;
            }

            float omega = TwoPi * settings.Frequency;
            float secondaryWeight = Math.Max(0f, settings.SecondaryWeight);

            float primary = (float)Math.Sin(
                (x + z * PrimaryShear) * settings.SpatialScale + omega * time);
            float secondary = (float)Math.Sin(
                (z + x * SecondaryShear) * settings.SpatialScale * SecondarySpatialScaleFactor
                - omega * time * SecondarySpeedFactor);

            return settings.Amplitude * (primary + secondaryWeight * secondary) / (1f + secondaryWeight);
        }
    }
}
