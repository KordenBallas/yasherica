using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Validates a rejoin claim at connection approval — implemented host-side over the seat
    /// ledger. Kept as a seam so the session service stays free of match knowledge.
    /// </summary>
    public interface IArenaRejoinGate
    {
        /// <summary>
        /// True if the token legitimately claims a gracing seat; the seat then holds in
        /// Resyncing on the new connection until the state transfer completes.
        /// </summary>
        bool TryApproveRejoin(int playerId, ulong token, ulong newClientId);
    }

    /// <summary>
    /// The engine-free surface of the network session the reconnect machinery drives (X1):
    /// host/join/shutdown, liveness events, and the rejoin gate. Production = the NGO session
    /// service; tests fake it to run drop/promote scenarios without a socket.
    /// </summary>
    public interface IArenaSessionControl
    {
        bool IsHost { get; }
        bool IsListening { get; }
        ulong LocalClientId { get; }

        /// <summary>Set when the host starts the match; fresh late joins are rejected from then on.</summary>
        bool MatchStarted { get; set; }

        IReadOnlyList<ulong> ConnectedClientIds { get; }

        event Action SessionStarted;
        event Action<ulong> ClientConnected;
        event Action<ulong> ClientDisconnected;

        bool StartHost();

        /// <summary>Joins <paramref name="address"/>; a non-null payload claims a rejoin (see
        /// <see cref="ArenaConnectPayload"/>).</summary>
        bool StartClient(string address, byte[] connectPayload = null);

        void Shutdown();

        /// <summary>Installs the host-side rejoin validator (null while not hosting).</summary>
        void SetRejoinGate(IArenaRejoinGate gate);
    }
}
