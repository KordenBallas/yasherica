namespace Inventory.Core
{
    /// <summary>
    /// Tunables for the idle drift of pot bubbles, kept as a plain struct so the
    /// drift calculator stays UnityEngine-free. Values are mapped from InventoryConfig.
    /// </summary>
    public readonly struct BubbleDriftSettings
    {
        /// <summary>Peak per-axis displacement from the bubble's base point.</summary>
        public float Amplitude { get; }

        /// <summary>Drift speed in cycles per second of the primary axis.</summary>
        public float Frequency { get; }

        public BubbleDriftSettings(float amplitude, float frequency)
        {
            Amplitude = amplitude;
            Frequency = frequency;
        }
    }
}
