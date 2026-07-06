namespace Combat.Arena.Core
{
    /// <summary>
    /// The host's pick-deadline time source, seamed out so the timeout auto-pick rule is
    /// edit-mode testable. Unity impl reads unscaled time; tests advance a fake.
    /// </summary>
    public interface IArenaDraftClock
    {
        float Now { get; }
    }
}
