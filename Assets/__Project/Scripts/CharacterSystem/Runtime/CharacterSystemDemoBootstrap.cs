using System.Collections.Generic;
using System.Text;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Demo-only adapter for manually verifying the character system in play mode.
    /// Creates a character on Start and exposes context-menu actions to exercise
    /// the runtime API while animations are playing.
    /// </summary>
    public class CharacterSystemDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterAssemblyDefinition _assembly;

        [Header("Manual test data (context menu)")]
        [SerializeField] private List<PartDefinition> _alternateParts;
        [SerializeField] private List<AttachmentDefinition> _attachments;

        [Inject] private IModularCharacterFactory _factory;
        [Inject] private IGameLogger _logger;

        private ModularCharacter _character;
        private readonly List<AttachmentHandle> _attachedHandles = new List<AttachmentHandle>();

        private void Start()
        {
            if (_assembly == null)
            {
                _logger.Error("[CharacterSystemDemo] No CharacterAssemblyDefinition assigned.");
                return;
            }

            _character = _factory.Create(_assembly, transform);
        }

        [ContextMenu("Swap To Alternate Parts")]
        private void SwapToAlternateParts()
        {
            if (_character == null)
            {
                return;
            }

            foreach (var part in _alternateParts)
            {
                if (part != null)
                {
                    _character.SwapPart(part);
                }
            }

            LogAvailableSockets();
        }

        [ContextMenu("Swap Back To Default Parts")]
        private void SwapBackToDefaultParts()
        {
            if (_character == null || _assembly == null)
            {
                return;
            }

            foreach (var part in _assembly.Parts)
            {
                if (part != null)
                {
                    _character.SwapPart(part);
                }
            }

            LogAvailableSockets();
        }

        [ContextMenu("Attach All Listed Attachments")]
        private void AttachAllListedAttachments()
        {
            if (_character == null)
            {
                return;
            }

            foreach (var attachment in _attachments)
            {
                if (attachment == null)
                {
                    continue;
                }

                var handle = _character.AttachToSocket(attachment);
                if (handle != null)
                {
                    _attachedHandles.Add(handle);
                }
            }
        }

        [ContextMenu("Detach All Attached")]
        private void DetachAllAttached()
        {
            if (_character == null)
            {
                return;
            }

            foreach (var handle in _attachedHandles)
            {
                _character.DetachFromSocket(handle);
            }

            _attachedHandles.Clear();
        }

        [ContextMenu("Log Available Sockets")]
        private void LogAvailableSockets()
        {
            if (_character == null)
            {
                return;
            }

            var sockets = _character.GetAvailableSockets();
            var builder = new StringBuilder($"[CharacterSystemDemo] {sockets.Count} available socket(s):\n");
            foreach (var socket in sockets)
            {
                builder.AppendLine($"  {socket}");
            }

            _logger.Info(builder.ToString());
        }
    }
}
