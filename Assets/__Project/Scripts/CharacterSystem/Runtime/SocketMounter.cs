using System;
using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Owns the socket transforms and attachment instances of one character.
    /// Applies socket diffs computed by the Core SocketCatalog: creates/destroys
    /// socket transforms under their parent bones and enforces the attachment policy
    /// when Tier-2 sockets disappear (re-parent if the socket id survives the swap,
    /// destroy otherwise).
    /// </summary>
    public class SocketMounter
    {
        private readonly ICharacterRig _rig;
        private readonly IGameLogger _logger;

        private readonly Dictionary<string, Transform> _socketTransforms = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AttachmentHandle>> _attachmentsBySocket = new Dictionary<string, List<AttachmentHandle>>(StringComparer.Ordinal);

        private int _nextHandleId = 1;

        public SocketMounter(ICharacterRig rig, IGameLogger logger)
        {
            _rig = rig;
            _logger = logger;
        }

        public Transform GetSocketTransform(string socketId)
        {
            return socketId != null && _socketTransforms.TryGetValue(socketId, out var socket) ? socket : null;
        }

        public void ApplyDiff(
            IReadOnlyList<SocketInfo> added,
            IReadOnlyList<SocketInfo> removed,
            Func<string, SocketDefinition> definitionLookup)
        {
            var recreatedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in added)
            {
                recreatedIds.Add(socket.Id);
            }

            foreach (var socket in removed)
            {
                RemoveSocket(socket, keepAttachments: recreatedIds.Contains(socket.Id));
            }

            foreach (var socket in added)
            {
                CreateSocket(socket, definitionLookup(socket.Id));
            }
        }

        public AttachmentHandle Attach(string socketId, GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            if (prefab == null)
            {
                _logger.Error($"[SocketMounter] Cannot attach a null prefab to socket '{socketId}'.");
                return null;
            }

            if (!_socketTransforms.TryGetValue(socketId, out var socket))
            {
                _logger.Error($"[SocketMounter] Socket '{socketId}' has no transform; is it available on this character?");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, socket, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;

            var handle = new AttachmentHandle(_nextHandleId++, socketId, instance);
            GetOrCreateAttachmentList(socketId).Add(handle);
            return handle;
        }

        public bool Detach(AttachmentHandle handle)
        {
            if (handle == null || !_attachmentsBySocket.TryGetValue(handle.SocketId, out var handles) || !handles.Remove(handle))
            {
                return false;
            }

            if (handle.Instance != null)
            {
                UnityEngine.Object.Destroy(handle.Instance);
            }

            return true;
        }

        public bool DetachAll(string socketId)
        {
            if (socketId == null || !_attachmentsBySocket.TryGetValue(socketId, out var handles) || handles.Count == 0)
            {
                return false;
            }

            foreach (var handle in handles)
            {
                if (handle.Instance != null)
                {
                    UnityEngine.Object.Destroy(handle.Instance);
                }
            }

            handles.Clear();
            return true;
        }

        private void CreateSocket(SocketInfo info, SocketDefinition definition)
        {
            if (!_rig.TryGetBone(info.ParentBoneName, out var parentBone))
            {
                _logger.Error($"[SocketMounter] Socket '{info.Id}' references missing bone '{info.ParentBoneName}'; socket skipped.");
                return;
            }

            var socketObject = new GameObject($"Socket_{info.Id}");
            var socketTransform = socketObject.transform;
            socketTransform.SetParent(parentBone, false);

            if (definition != null)
            {
                socketTransform.localPosition = definition.LocalPosition;
                socketTransform.localRotation = definition.LocalRotation;
                socketTransform.localScale = definition.LocalScale;
            }

            _socketTransforms[info.Id] = socketTransform;

            // Attachments preserved across a same-id socket recreation move to the new transform.
            if (_attachmentsBySocket.TryGetValue(info.Id, out var pending))
            {
                foreach (var handle in pending)
                {
                    if (handle.Instance != null)
                    {
                        handle.Instance.transform.SetParent(socketTransform, false);
                    }
                }
            }
        }

        private void RemoveSocket(SocketInfo info, bool keepAttachments)
        {
            if (!_socketTransforms.TryGetValue(info.Id, out var socketTransform))
            {
                return;
            }

            _socketTransforms.Remove(info.Id);

            if (_attachmentsBySocket.TryGetValue(info.Id, out var handles) && handles.Count > 0)
            {
                if (keepAttachments)
                {
                    // Park attachments on the rig root until CreateSocket re-parents them.
                    foreach (var handle in handles)
                    {
                        if (handle.Instance != null)
                        {
                            handle.Instance.transform.SetParent(_rig.Root, false);
                        }
                    }
                }
                else
                {
                    _logger.Info($"[SocketMounter] Socket '{info.Id}' disappeared with its part; destroying {handles.Count} attachment(s).");
                    foreach (var handle in handles)
                    {
                        if (handle.Instance != null)
                        {
                            UnityEngine.Object.Destroy(handle.Instance);
                        }
                    }

                    handles.Clear();
                }
            }

            if (socketTransform != null)
            {
                UnityEngine.Object.Destroy(socketTransform.gameObject);
            }
        }

        private List<AttachmentHandle> GetOrCreateAttachmentList(string socketId)
        {
            if (!_attachmentsBySocket.TryGetValue(socketId, out var handles))
            {
                handles = new List<AttachmentHandle>();
                _attachmentsBySocket.Add(socketId, handles);
            }

            return handles;
        }
    }
}
