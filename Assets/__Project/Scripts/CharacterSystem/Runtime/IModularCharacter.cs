using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Public runtime API of an assembled modular character. All operations are safe
    /// to call during gameplay while animations are playing.
    /// </summary>
    public interface IModularCharacter
    {
        /// <summary>Replaces the part in a slot, resolving the part id through the part catalog.</summary>
        bool SwapPart(string slotId, string partId);

        bool SwapPart(PartDefinition part);

        AttachmentHandle AttachToSocket(string socketId, GameObject prefab);

        AttachmentHandle AttachToSocket(AttachmentDefinition attachment);

        bool DetachFromSocket(AttachmentHandle handle);

        /// <summary>Detaches everything currently attached to the socket.</summary>
        bool DetachFromSocket(string socketId);

        /// <summary>Currently equipped parts as a slotId -&gt; partId map (snapshot).</summary>
        IReadOnlyDictionary<string, string> EquippedParts { get; }

        /// <summary>Tier-1 skeleton sockets plus Tier-2 sockets contributed by currently equipped parts.</summary>
        IReadOnlyList<SocketInfo> GetAvailableSockets();

        Transform GetSocketTransform(string socketId);
    }
}
