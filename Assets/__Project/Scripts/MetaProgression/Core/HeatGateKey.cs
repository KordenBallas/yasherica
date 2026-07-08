namespace MetaProgression.Core
{
    /// <summary>
    /// Which Heat reading a min-Heat gate compares against (heat-ascension FR5, authored per gate):
    /// the pact currently in force, or the persisted hottest clear (the mastery record).
    /// </summary>
    public enum HeatGateKey
    {
        CurrentPact = 0,
        HighWaterMark = 1
    }
}
