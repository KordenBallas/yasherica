using System;
using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// The brain of one assembled character. Coordinates the pure-C# state
    /// (assembly state + socket catalog) with the Unity-side executors
    /// (part swap, socket mounting). Pure C# class; never a MonoBehaviour.
    /// </summary>
    public class CharacterAssemblyController : IDisposable
    {
        private readonly SkeletonDefinition _skeletonDefinition;
        private readonly SkeletonData _skeleton;
        private readonly IPartCatalog _partCatalog;
        private readonly PartSwapExecutor _swapExecutor;
        private readonly SocketMounter _socketMounter;
        private readonly IGameLogger _logger;
        private readonly AssemblyValidator _validator = new AssemblyValidator();

        private readonly CharacterAssemblyState _state = new CharacterAssemblyState();

        /// <summary>Raised after a successful part swap, once the assembly state and sockets are
        /// rebuilt. Consumers (e.g. the race passport projector) re-read <see cref="EquippedParts"/>.</summary>
        public event Action PartsChanged;
        private readonly SocketCatalog _socketCatalog = new SocketCatalog();
        private readonly Dictionary<string, GameObject> _partInstancesBySlot = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, PartDefinition> _partDefinitionsBySlot = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);

        // Losing frame-changers: equipped-but-dormant (body-plan-skeleton-swap.md FR2). They
        // contribute no renderer, sockets, or abilities — only governance candidacy — so they
        // are tracked outside _state and never surface in EquippedParts.
        private readonly Dictionary<string, PartDefinition> _dormantBySlot = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);

        public CharacterAssemblyController(
            SkeletonDefinition skeletonDefinition,
            IPartCatalog partCatalog,
            PartSwapExecutor swapExecutor,
            SocketMounter socketMounter,
            IGameLogger logger)
        {
            _skeletonDefinition = skeletonDefinition;
            _skeleton = DefinitionMapper.ToSkeletonData(skeletonDefinition);
            _partCatalog = partCatalog;
            _swapExecutor = swapExecutor;
            _socketMounter = socketMounter;
            _logger = logger;

            _socketCatalog.Changed += HandleSocketCatalogChanged;
        }

        /// <summary>Mounts the Tier-1 sockets. Called once by the factory right after construction.</summary>
        public void MountSkeletonSockets()
        {
            _socketCatalog.Rebuild(_skeleton, _state.EquippedParts);
        }

        public bool SwapPart(string slotId, string partId)
        {
            if (!_partCatalog.TryGet(partId, out var part))
            {
                _logger.Error(LogCategory.CharacterSystem,$"[CharacterAssembly] Unknown part id '{partId}'.");
                return false;
            }

            if (part.Slot == null || !string.Equals(part.Slot.Id, slotId, StringComparison.Ordinal))
            {
                _logger.Error(LogCategory.CharacterSystem,$"[CharacterAssembly] Part '{partId}' belongs to slot '{part.Slot?.Id}', not '{slotId}'.");
                return false;
            }

            return SwapPart(part);
        }

        public bool SwapPart(PartDefinition part)
        {
            if (part == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[CharacterAssembly] Cannot swap a null part definition.");
                return false;
            }

            var partData = DefinitionMapper.ToPartData(part);
            if (!ValidateForSwap(partData))
            {
                return false;
            }

            _partInstancesBySlot.TryGetValue(partData.SlotId, out var oldInstance);
            var newInstance = _swapExecutor.Swap(partData, part.PartPrefab, oldInstance);
            if (newInstance == null)
            {
                return false;
            }

            _partInstancesBySlot[partData.SlotId] = newInstance;
            _partDefinitionsBySlot[partData.SlotId] = part;
            // A slot holds at most one part, active or dormant: an active install into a
            // dormant-held slot replaces the dormant occupant.
            _dormantBySlot.Remove(partData.SlotId);
            _state.Equip(partData);
            _socketCatalog.Rebuild(_skeleton, _state.EquippedParts);
            PartsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Records a losing frame-changer as equipped-but-dormant: state only, no renderer,
        /// no sockets. Any active or dormant occupant of the slot is replaced (an active
        /// occupant's instance is destroyed, mirroring an ordinary swap's replacement).
        /// </summary>
        public bool EquipDormant(PartDefinition part)
        {
            if (part == null || part.Slot == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[CharacterAssembly] Cannot equip a null or slotless part as dormant.");
                return false;
            }

            var slotId = part.Slot.Id;
            if (_partInstancesBySlot.TryGetValue(slotId, out var activeInstance))
            {
                if (activeInstance != null)
                {
                    UnityEngine.Object.Destroy(activeInstance);
                }

                _partInstancesBySlot.Remove(slotId);
                _partDefinitionsBySlot.Remove(slotId);
                _state.Remove(slotId);
                _socketCatalog.Rebuild(_skeleton, _state.EquippedParts);
            }

            _dormantBySlot[slotId] = part;
            PartsChanged?.Invoke();
            return true;
        }

        /// <summary>Losing frame-changers currently carried as dormant (governance candidates only).</summary>
        public IReadOnlyCollection<PartDefinition> DormantParts => _dormantBySlot.Values;

        /// <summary>Definitions of the parts rendered on the body (excludes dormant parts).</summary>
        public IReadOnlyCollection<PartDefinition> EquippedPartDefinitions => _partDefinitionsBySlot.Values;

        /// <summary>Id of the skeleton this body is assembled on.</summary>
        public string SkeletonId => _skeleton.SkeletonId;

        public AttachmentHandle AttachToSocket(string socketId, GameObject prefab)
        {
            if (!_socketCatalog.Contains(socketId))
            {
                _logger.Error(LogCategory.CharacterSystem,$"[CharacterAssembly] Socket '{socketId}' is not available on this character.");
                return null;
            }

            return _socketMounter.Attach(socketId, prefab, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public AttachmentHandle AttachToSocket(AttachmentDefinition attachment)
        {
            if (attachment == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[CharacterAssembly] Cannot attach a null attachment definition.");
                return null;
            }

            if (!_socketCatalog.Contains(attachment.SocketId))
            {
                _logger.Error(LogCategory.CharacterSystem,$"[CharacterAssembly] Attachment '{attachment.Id}' targets socket '{attachment.SocketId}' which is not available on this character.");
                return null;
            }

            return _socketMounter.Attach(
                attachment.SocketId,
                attachment.Prefab,
                attachment.LocalPosition,
                attachment.LocalRotation,
                attachment.LocalScale);
        }

        public bool DetachFromSocket(AttachmentHandle handle)
        {
            return _socketMounter.Detach(handle);
        }

        public bool DetachFromSocket(string socketId)
        {
            return _socketMounter.DetachAll(socketId);
        }

        public IReadOnlyDictionary<string, string> EquippedParts => _state.EquippedPartIdsBySlot();

        public IReadOnlyList<SocketInfo> GetAvailableSockets()
        {
            return _socketCatalog.AvailableSockets;
        }

        public Transform GetSocketTransform(string socketId)
        {
            return _socketMounter.GetSocketTransform(socketId);
        }

        public void Dispose()
        {
            _socketCatalog.Changed -= HandleSocketCatalogChanged;
        }

        private bool ValidateForSwap(PartData partData)
        {
            var issues = _validator.ValidatePart(partData, _skeleton);
            var hasErrors = false;
            foreach (var issue in issues)
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    hasErrors = true;
                    _logger.Error(LogCategory.CharacterSystem,$"[CharacterAssembly] {issue}");
                }
                else
                {
                    _logger.Warning(LogCategory.CharacterSystem,$"[CharacterAssembly] {issue}");
                }
            }

            return !hasErrors;
        }

        private void HandleSocketCatalogChanged(IReadOnlyList<SocketInfo> added, IReadOnlyList<SocketInfo> removed)
        {
            _socketMounter.ApplyDiff(added, removed, FindSocketDefinition);
        }

        private SocketDefinition FindSocketDefinition(string socketId)
        {
            foreach (var socket in _skeletonDefinition.Tier1Sockets)
            {
                if (socket != null && string.Equals(socket.Id, socketId, StringComparison.Ordinal))
                {
                    return socket;
                }
            }

            foreach (var part in _partDefinitionsBySlot.Values)
            {
                foreach (var socket in part.ContributedSockets)
                {
                    if (socket != null && string.Equals(socket.Id, socketId, StringComparison.Ordinal))
                    {
                        return socket;
                    }
                }
            }

            return null;
        }
    }
}
