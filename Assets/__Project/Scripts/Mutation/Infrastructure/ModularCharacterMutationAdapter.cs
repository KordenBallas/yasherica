using System;
using CharacterSystem.Runtime;
using Core.Logging;
using Mutation.Core;

namespace Mutation.Infrastructure
{
    /// <summary>
    /// Adapts the body-plan swap coordinator (and the scene's <see cref="ModularCharacterVisual"/>)
    /// to the UnityEngine-free <see cref="IMutationCharacter"/> port. Every install request is
    /// routed through the coordinator so a frame-changing part triggers the confirm-and-shed flow
    /// while ordinary parts keep the instant same-frame swap. Everything resolves at call time:
    /// an unassembled rig logs/rejects rather than throwing. The equipped-part query reads the
    /// live body including dormant frame-changers, so the option builder excludes every part
    /// the character is carrying.
    /// </summary>
    public sealed class ModularCharacterMutationAdapter : IMutationCharacter, IDisposable
    {
        private readonly BodyPlanSwapCoordinator _coordinator;
        private readonly ModularCharacterVisual _visual;
        private readonly IGameLogger _logger;

        public event Action<bool> SwapRequestResolved;

        public ModularCharacterMutationAdapter(
            BodyPlanSwapCoordinator coordinator,
            ModularCharacterVisual visual,
            IGameLogger logger)
        {
            _coordinator = coordinator;
            _visual = visual;
            _logger = logger;
            _coordinator.InstallResolved += HandleInstallResolved;
        }

        public void Dispose()
        {
            _coordinator.InstallResolved -= HandleInstallResolved;
        }

        public SwapRequestOutcome RequestSwapPart(string slotId, string partId)
        {
            var character = _visual != null ? _visual.Character : null;
            if (character == null)
            {
                _logger.Warning(LogCategory.Mutation,
                    $"[ModularCharacterMutationAdapter] No assembled character yet; cannot install " +
                    $"'{partId}' into '{slotId}'.");
                return SwapRequestOutcome.Rejected;
            }

            switch (_coordinator.RequestInstall(slotId, partId))
            {
                case BodyPlanInstallOutcome.Applied:
                    return SwapRequestOutcome.Applied;
                case BodyPlanInstallOutcome.PendingConfirmation:
                    return SwapRequestOutcome.PendingConfirmation;
                default:
                    return SwapRequestOutcome.Rejected;
            }
        }

        public bool CanInstall(string partId)
        {
            return _coordinator.CanInstall(partId);
        }

        public bool TryGetEquippedPartId(string slotId, out string partId)
        {
            partId = null;
            if (string.IsNullOrEmpty(slotId))
            {
                return false;
            }

            // Read the live body so starting parts (never swapped this run) are excluded too.
            // An unassembled rig has no equipped parts yet -> unknown (false).
            var character = _visual != null ? _visual.Character : null;
            if (character == null)
            {
                return false;
            }

            if (character.EquippedParts.TryGetValue(slotId, out partId))
            {
                return true;
            }

            // A losing frame-changer keeps its slot as a dormant occupant (FR2), so it must
            // not be offered again either.
            foreach (var dormant in character.DormantParts)
            {
                if (dormant != null && dormant.Slot != null
                    && string.Equals(dormant.Slot.Id, slotId, StringComparison.Ordinal))
                {
                    partId = dormant.Id;
                    return true;
                }
            }

            return false;
        }

        private void HandleInstallResolved(bool installed)
        {
            SwapRequestResolved?.Invoke(installed);
        }
    }
}
