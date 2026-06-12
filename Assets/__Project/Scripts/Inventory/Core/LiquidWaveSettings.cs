namespace Inventory.Core
{
    /// <summary>
    /// Tunables for the boiling liquid surface, kept as a plain struct so the wave
    /// calculator stays UnityEngine-free. Values are mapped from InventoryConfig.
    /// </summary>
    public readonly struct LiquidWaveSettings
    {
        /// <summary>Peak vertical displacement of the surface.</summary>
        public float Amplitude { get; }

        /// <summary>Boil speed in oscillations per second.</summary>
        public float Frequency { get; }

        /// <summary>How tightly the waves ripple across the surface (higher = denser).</summary>
        public float SpatialScale { get; }

        /// <summary>Blend of a second crossed wave that breaks up the primary pattern.</summary>
        public float SecondaryWeight { get; }

        public LiquidWaveSettings(float amplitude, float frequency, float spatialScale, float secondaryWeight)
        {
            Amplitude = amplitude;
            Frequency = frequency;
            SpatialScale = spatialScale;
            SecondaryWeight = secondaryWeight;
        }
    }
}
