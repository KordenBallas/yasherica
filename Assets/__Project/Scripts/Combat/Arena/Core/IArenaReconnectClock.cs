namespace Combat.Arena.Core
{
    /// <summary>
    /// The reconnect state machine's time source, seamed out so retry windows and timeouts are
    /// edit-mode testable (the <see cref="IArenaDraftClock"/> pattern). Unity impl reads
    /// unscaled time; tests advance a fake.
    /// </summary>
    public interface IArenaReconnectClock
    {
        float Now { get; }
    }

    /// <summary>Where a client learns its own reachable address for the migration book (LAN best-effort).</summary>
    public interface IArenaLocalEndpointSource
    {
        /// <summary>The local machine's dialable address, or null when none can be claimed.</summary>
        string GetLocalAddress();
    }
}
