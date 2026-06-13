using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Identifies one live attachment instance so callers can detach exactly
    /// what they attached, even when several props share a socket.
    /// </summary>
    public sealed class AttachmentHandle
    {
        public int Id { get; }
        public string SocketId { get; }
        public GameObject Instance { get; }

        public AttachmentHandle(int id, string socketId, GameObject instance)
        {
            Id = id;
            SocketId = socketId;
            Instance = instance;
        }
    }
}
