using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using Core.Logging;
using Zenject;

namespace CharacterSystem.View
{
    /// <summary>
    /// Drives the body-plan demo console: one dropdown per authored slot, listing every
    /// catalog part of that slot (frame-changers tagged "[frame]", the dormant occupant
    /// tagged "(dormant)"). Every pick routes through the <see cref="BodyPlanSwapCoordinator"/>,
    /// so ordinary parts swap instantly while frame-changing picks run the real
    /// confirm-and-shed flow. The console re-renders on every body change; a rejected or
    /// declined pick simply re-renders back to the actual body (the dropdown reverts).
    /// Pure C#; never a MonoBehaviour. Dev tooling — not part of the shipped game loop.
    /// </summary>
    public class BodyPlanDemoConsolePresenter : IInitializable, IDisposable
    {
        private readonly IPartCatalog _partCatalog;
        private readonly ModularCharacterVisual _visual;
        private readonly BodyPlanSwapCoordinator _coordinator;
        private readonly IBodyPlanDemoConsoleView _view;
        private readonly IGameLogger _logger;

        private IModularCharacter _character;

        public BodyPlanDemoConsolePresenter(
            IPartCatalog partCatalog,
            ModularCharacterVisual visual,
            BodyPlanSwapCoordinator coordinator,
            IBodyPlanDemoConsoleView view,
            IGameLogger logger)
        {
            _partCatalog = partCatalog;
            _visual = visual;
            _coordinator = coordinator;
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            _view.OnPartPicked += HandlePartPicked;
            _coordinator.InstallResolved += HandleInstallResolved;
            _visual.CharacterAssembled += HandleCharacterAssembled;

            // Late-subscriber case: the rig may already be assembled.
            if (_visual.Character != null)
            {
                HandleCharacterAssembled(_visual.Character);
            }
        }

        public void Dispose()
        {
            _view.OnPartPicked -= HandlePartPicked;
            _coordinator.InstallResolved -= HandleInstallResolved;
            _visual.CharacterAssembled -= HandleCharacterAssembled;
            if (_character != null)
            {
                _character.PartsChanged -= Refresh;
            }
        }

        private void HandleCharacterAssembled(IModularCharacter character)
        {
            // A body-plan change replaces the whole character; re-bind the change listener.
            if (_character != null)
            {
                _character.PartsChanged -= Refresh;
            }

            _character = character;
            _character.PartsChanged += Refresh;
            Refresh();
        }

        private void HandleInstallResolved(bool installed)
        {
            // Declined/failed frame change: nothing on the body moved, but the dropdown
            // shows the attempted pick — re-render back to the actual body. A confirmed
            // change refreshes via CharacterAssembled/PartsChanged anyway; the extra
            // refresh is harmless.
            Refresh();
        }

        private void HandlePartPicked(string slotId, string partId)
        {
            if (_character == null)
            {
                return;
            }

            if (TryGetOccupant(slotId, out var occupantId, out _) && string.Equals(occupantId, partId, StringComparison.Ordinal))
            {
                return;
            }

            var outcome = _coordinator.RequestInstall(slotId, partId);
            if (outcome == BodyPlanInstallOutcome.Rejected)
            {
                _logger.Warning(LogCategory.CharacterSystem,
                    $"[BodyPlanDemo] Install of '{partId}' into '{slotId}' was rejected; reverting the dropdown.");
                Refresh();
            }

            // Applied → PartsChanged re-renders; PendingConfirmation → InstallResolved re-renders.
        }

        private void Refresh()
        {
            if (_character == null)
            {
                _view.ShowRows(Array.Empty<BodyPlanDemoRowViewData>());
                return;
            }

            var partsBySlot = new SortedDictionary<string, List<PartDefinition>>(StringComparer.Ordinal);
            var slotLabels = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in _partCatalog.All)
            {
                if (part == null || part.Slot == null)
                {
                    continue;
                }

                if (!partsBySlot.TryGetValue(part.Slot.Id, out var slotParts))
                {
                    slotParts = new List<PartDefinition>();
                    partsBySlot[part.Slot.Id] = slotParts;
                    slotLabels[part.Slot.Id] = string.IsNullOrEmpty(part.Slot.DisplayName) ? part.Slot.Id : part.Slot.DisplayName;
                }

                slotParts.Add(part);
            }

            var rows = new List<BodyPlanDemoRowViewData>(partsBySlot.Count);
            foreach (var entry in partsBySlot)
            {
                entry.Value.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                rows.Add(BuildRow(entry.Key, slotLabels[entry.Key], entry.Value));
            }

            _view.ShowRows(rows);
        }

        private BodyPlanDemoRowViewData BuildRow(string slotId, string slotLabel, List<PartDefinition> parts)
        {
            var hasOccupant = TryGetOccupant(slotId, out var occupantId, out var occupantIsDormant);

            var labels = new List<string>(parts.Count + 1);
            var partIds = new List<string>(parts.Count + 1);
            if (!hasOccupant)
            {
                labels.Add("(empty)");
                partIds.Add(null);
            }

            var selectedIndex = 0;
            foreach (var part in parts)
            {
                var label = string.IsNullOrEmpty(part.DisplayName) ? part.name : part.DisplayName;
                if (part.GovernsBodyPlan)
                {
                    label += " [frame]";
                }

                if (hasOccupant && string.Equals(part.Id, occupantId, StringComparison.Ordinal))
                {
                    selectedIndex = partIds.Count;
                    if (occupantIsDormant)
                    {
                        label += " (dormant)";
                    }
                }

                labels.Add(label);
                partIds.Add(part.Id);
            }

            return new BodyPlanDemoRowViewData(slotId, slotLabel, labels, partIds, selectedIndex);
        }

        /// <summary>The slot's occupant on the live body: the rendered part or, failing that,
        /// a dormant frame-changer holding the slot (FR2).</summary>
        private bool TryGetOccupant(string slotId, out string partId, out bool isDormant)
        {
            partId = null;
            isDormant = false;

            if (_character.EquippedParts.TryGetValue(slotId, out partId))
            {
                return true;
            }

            foreach (var dormant in _character.DormantParts)
            {
                if (dormant != null && dormant.Slot != null
                    && string.Equals(dormant.Slot.Id, slotId, StringComparison.Ordinal))
                {
                    partId = dormant.Id;
                    isDormant = true;
                    return true;
                }
            }

            return false;
        }
    }
}
