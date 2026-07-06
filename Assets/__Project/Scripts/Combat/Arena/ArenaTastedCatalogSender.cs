using System;
using Combat.Arena.Core;
using Combat.Arena.Data;
using Combat.Arena.Networking;
using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// Client-side half of the catalog exchange: as soon as the session is up (host and joiner
    /// alike), the local tasted-forms catalog goes to the host's registry. Offline play has no
    /// session — the entrypoint calls <see cref="SubmitNow"/> before opening the draft instead.
    /// </summary>
    public sealed class ArenaTastedCatalogSender : IInitializable, IDisposable
    {
        private readonly ArenaSessionService _session;
        private readonly IArenaTransport _transport;
        private readonly ArenaTastedCatalogReader _reader;

        public ArenaTastedCatalogSender(
            ArenaSessionService session,
            IArenaTransport transport,
            ArenaTastedCatalogReader reader)
        {
            _session = session;
            _transport = transport;
            _reader = reader;
        }

        public void Initialize()
        {
            _session.SessionStarted += SubmitNow;
        }

        public void Dispose()
        {
            _session.SessionStarted -= SubmitNow;
        }

        public void SubmitNow()
        {
            _transport.SubmitTastedCatalog(_reader.ReadLocalCatalog());
        }
    }
}
