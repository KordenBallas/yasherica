using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// Frame pump for the reconnect state machine — the one Zenject-facing shim, so
    /// <see cref="ArenaReconnectClient"/> itself stays framework-free and test-tickable.
    /// </summary>
    public class ArenaReconnectTicker : ITickable
    {
        private readonly ArenaReconnectClient _client;

        public ArenaReconnectTicker(ArenaReconnectClient client)
        {
            _client = client;
        }

        public void Tick()
        {
            _client.Tick();
        }
    }
}
