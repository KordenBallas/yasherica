using System;
using System.Collections.Generic;
using Inventory.Core;

namespace Mutation.Core
{
    /// <summary>
    /// The operating table's socketing state: which artifacts sit in which racked
    /// blank's sockets. Socketing pulls the artifact out of the inventory (the
    /// crafting-staging pattern); unsocketing returns it. Filling the last socket
    /// raises <see cref="OnBlankReady"/> — the medallion's rim-closed signal.
    /// Rearranging stays free until the player confirms the unseal (Track F);
    /// the confirm consumes the sockets (commit-on-unseal).
    /// </summary>
    public interface ISocketingModel
    {
        /// <summary>Raised whenever a blank's socket content changes.</summary>
        event Action<int> OnSocketsChanged;

        /// <summary>Raised when the last socket of a blank is filled (the rim closes).</summary>
        event Action<int> OnBlankReady;

        /// <summary>Artifacts socketed into the given blank, in socketing order.</summary>
        IReadOnlyList<ArtifactInstance> SocketedArtifacts(int blankInstanceId);

        /// <summary>True when every socket of the blank is filled.</summary>
        bool IsReady(int blankInstanceId);

        /// <summary>
        /// Moves the inventory artifact into the blank's next open socket.
        /// Returns false when the blank is unknown/full or the artifact is not
        /// in the inventory.
        /// </summary>
        bool TrySocket(int blankInstanceId, int artifactInstanceId);

        /// <summary>
        /// Returns a socketed artifact to the inventory. Allowed even on a ready
        /// blank (it reopens); only the unseal confirm commits.
        /// </summary>
        bool TryUnsocket(int blankInstanceId, int artifactInstanceId);

        /// <summary>
        /// Raised when an unseal confirm consumes a blank's socketed reagents — the crafting
        /// commit point. The meta-progression ledger records the consumed definition ids here
        /// (the artifact half of the cross-run direction tally).
        /// </summary>
        event Action<IReadOnlyList<ArtifactInstance>> OnSocketsConsumed;

        /// <summary>
        /// Consumes the blank's socketed artifacts (they are destroyed, not
        /// returned) and clears its socket state. Called when the player picks
        /// an unseal variant. Returns the consumed artifacts.
        /// </summary>
        IReadOnlyList<ArtifactInstance> ConsumeSockets(int blankInstanceId);

        /// <summary>Returns every non-committed socketed artifact to the inventory (on close).</summary>
        void ReturnAll();
    }
}
