namespace Core.Persistence
{
    /// <summary>
    /// The one-shot Hub-arrival marker (<c>hub-arrival.json</c>): the death hook marks it in the
    /// Area scene, the Hub consumes it once to colour the arrival (the death-return voice line).
    /// Missing/corrupt = a plain visit — the marker is presentation-only and must never gate flow.
    /// </summary>
    public interface IHubArrivalStore
    {
        void MarkDeathReturn();

        /// <summary>True when a death return was marked; the marker is consumed either way.</summary>
        bool TryConsumeDeathReturn();
    }
}
