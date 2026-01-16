namespace Combat.Data.Definitions
{
    /// <summary>
    /// Defines the direction of HP threshold comparison for status effect triggers.
    /// </summary>
    public enum ThresholdDirection
    {
        /// <summary>Trigger when HP falls below the threshold.</summary>
        Below,

        /// <summary>Trigger when HP rises above the threshold.</summary>
        Above
    }
}
