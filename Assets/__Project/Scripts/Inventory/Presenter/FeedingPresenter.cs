using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Inventory.View;
using Mutation.Core;
using Mutation.Data;
using UnityEngine;
using Zenject;

namespace Inventory.Presenter
{
    /// <summary>
    /// Drives the feeding flow: in feeding mode, routes pot-bubble clicks into the feeding tray,
    /// shows the cumulative archetype readout for the tray, and on Feed digests each tray artifact
    /// into the mutation tally and digestion progress. Reads which mode is active so it ignores
    /// clicks that belong to the crafting flow. Holds no domain state of its own.
    /// </summary>
    public class FeedingPresenter : IInitializable, IDisposable
    {
        // The dominant-archetype panel shows the strongest few axes this stage.
        private const int DominantCount = 3;

        private readonly IFeedingSession _session;
        private readonly IFeedingView _view;
        private readonly IPotView _potView;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly IArchetypeCatalog _archetypeCatalog;
        private readonly IMutationTally _tally;
        private readonly IDigestionProgress _digestion;
        private readonly IInventoryModeState _modeState;
        private readonly IGameLogger _logger;

        public FeedingPresenter(
            IFeedingSession session,
            IFeedingView view,
            IPotView potView,
            IArtifactCatalog artifactCatalog,
            IArchetypeCatalog archetypeCatalog,
            IMutationTally tally,
            IDigestionProgress digestion,
            IInventoryModeState modeState,
            IGameLogger logger)
        {
            _session = session;
            _view = view;
            _potView = potView;
            _artifactCatalog = artifactCatalog;
            _archetypeCatalog = archetypeCatalog;
            _tally = tally;
            _digestion = digestion;
            _modeState = modeState;
            _logger = logger;
        }

        public void Initialize()
        {
            _potView.OnBubbleClicked += HandleBubbleClicked;
            _view.OnTrayItemClicked += HandleTrayItemClicked;
            _view.OnFeedClicked += HandleFeedClicked;
            _session.OnTrayChanged += HandleTrayChanged;
            _tally.OnChanged += RefreshStageReadout;
            _digestion.OnChanged += RefreshStageReadout;
            _modeState.OnModeChanged += HandleModeChanged;

            _view.SetVisible(_modeState.Mode == InventoryMode.Feeding);
            RefreshTray();
            RefreshStageReadout();
        }

        public void Dispose()
        {
            _potView.OnBubbleClicked -= HandleBubbleClicked;
            _view.OnTrayItemClicked -= HandleTrayItemClicked;
            _view.OnFeedClicked -= HandleFeedClicked;
            _session.OnTrayChanged -= HandleTrayChanged;
            _tally.OnChanged -= RefreshStageReadout;
            _digestion.OnChanged -= RefreshStageReadout;
            _modeState.OnModeChanged -= HandleModeChanged;
        }

        private void HandleBubbleClicked(int instanceId)
        {
            // Crafting owns clicks in crafting mode; this presenter only feeds.
            if (_modeState.Mode != InventoryMode.Feeding)
            {
                return;
            }

            if (!_session.TrySelect(instanceId))
            {
                _logger.Info($"[FeedingPresenter] Selection of instance {instanceId} rejected.");
            }
        }

        private void HandleTrayItemClicked(int instanceId)
        {
            if (!_session.TryUnselect(instanceId))
            {
                _logger.Info($"[FeedingPresenter] Unselecting of instance {instanceId} rejected.");
            }
        }

        private void HandleFeedClicked()
        {
            var eaten = _session.Consume();
            foreach (var instance in eaten)
            {
                // The artifacts already left the inventory when selected; digest them now.
                _tally.Add(ProfileFor(instance.DefinitionId));
                _digestion.AddArtifact();
            }
        }

        private void HandleModeChanged(InventoryMode mode)
        {
            if (mode != InventoryMode.Feeding)
            {
                // Leaving feeding mode drops the tray back into the pot.
                _session.ReturnAll();
            }

            _view.SetVisible(mode == InventoryMode.Feeding);
        }

        private void HandleTrayChanged()
        {
            RefreshTray();
        }

        private void RefreshTray()
        {
            var tray = _session.Tray;
            var viewData = new List<ArtifactViewData>(tray.Count);
            var profiles = new List<ArtifactArchetypeProfile>(tray.Count);
            foreach (var instance in tray)
            {
                viewData.Add(ToViewData(instance));
                profiles.Add(ProfileFor(instance.DefinitionId));
            }

            _view.ShowTray(viewData);
            _view.SetFeedEnabled(tray.Count > 0);
            _view.SetCumulativeReadout(ToReadout(ArtifactArchetypeProfile.Combine(profiles).Weights));
        }

        private void RefreshStageReadout()
        {
            _view.SetProgression(_digestion.Fed, _digestion.Threshold, _digestion.Normalized);

            var dominant = _tally.Dominant(DominantCount);
            var entries = new List<ArchetypeReadoutEntry>(dominant.Count);
            foreach (var id in dominant)
            {
                entries.Add(ToReadoutEntry(id, _tally.TotalFor(id)));
            }

            _view.SetDominant(entries);
        }

        private IReadOnlyList<ArchetypeReadoutEntry> ToReadout(IReadOnlyDictionary<string, float> weights)
        {
            var entries = new List<ArchetypeReadoutEntry>(weights.Count);
            foreach (var pair in weights)
            {
                entries.Add(ToReadoutEntry(pair.Key, pair.Value));
            }

            return entries;
        }

        private ArchetypeReadoutEntry ToReadoutEntry(string archetypeId, float weight)
        {
            if (_archetypeCatalog.TryGet(archetypeId, out var definition))
            {
                return new ArchetypeReadoutEntry(definition.DisplayName, weight, definition.Tint);
            }

            return new ArchetypeReadoutEntry(archetypeId, weight, Color.white);
        }

        private ArtifactArchetypeProfile ProfileFor(string definitionId)
        {
            if (_artifactCatalog.TryGet(definitionId, out var definition))
            {
                return ArtifactArchetypeMapper.ToProfile(definition.ArchetypeWeights);
            }

            _logger.Warning($"[FeedingPresenter] No artifact definition for id '{definitionId}'.");
            return ArtifactArchetypeProfile.Empty;
        }

        private ArtifactViewData ToViewData(ArtifactInstance instance)
        {
            if (_artifactCatalog.TryGet(instance.DefinitionId, out var definition))
            {
                return new ArtifactViewData(instance.InstanceId, definition.Icon, definition.BubbleTint);
            }

            _logger.Warning($"[FeedingPresenter] No artifact definition for id '{instance.DefinitionId}'.");
            return new ArtifactViewData(instance.InstanceId, null, Color.white);
        }
    }
}
