using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Thin MonoBehaviour facade over the assembly controller so gameplay code can
    /// find "the character" as a component. Every method is pure delegation;
    /// the controller is created and injected by the factory.
    /// </summary>
    public class ModularCharacter : MonoBehaviour, IModularCharacter
    {
        private CharacterAssemblyController _controller;

        public void Initialize(CharacterAssemblyController controller)
        {
            _controller = controller;
        }

        public bool SwapPart(string slotId, string partId) => _controller.SwapPart(slotId, partId);

        public bool SwapPart(PartDefinition part) => _controller.SwapPart(part);

        public AttachmentHandle AttachToSocket(string socketId, GameObject prefab) => _controller.AttachToSocket(socketId, prefab);

        public AttachmentHandle AttachToSocket(AttachmentDefinition attachment) => _controller.AttachToSocket(attachment);

        public bool DetachFromSocket(AttachmentHandle handle) => _controller.DetachFromSocket(handle);

        public bool DetachFromSocket(string socketId) => _controller.DetachFromSocket(socketId);

        public IReadOnlyDictionary<string, string> EquippedParts => _controller.EquippedParts;

        public IReadOnlyList<SocketInfo> GetAvailableSockets() => _controller.GetAvailableSockets();

        public Transform GetSocketTransform(string socketId) => _controller.GetSocketTransform(socketId);

        private void OnDestroy()
        {
            _controller?.Dispose();
        }
    }
}
