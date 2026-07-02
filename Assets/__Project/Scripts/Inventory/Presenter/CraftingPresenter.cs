using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Inventory.View;
using UnityEngine;
using Zenject;

namespace Inventory.Presenter
{
    /// <summary>
    /// Drives the crafting flow above the pot: routes bubble and staged-item clicks
    /// into the crafting session, runs the merge-animation handshake (crafting started
    /// -> merge animation -> resolve), and renders staged items, combine results, and
    /// smoke puffs.
    /// </summary>
    public class CraftingPresenter : IInitializable, IDisposable
    {
        private readonly ICraftingSession _session;
        private readonly ICraftingSlotsView _slotsView;
        private readonly IPotView _potView;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly IInventoryModeState _modeState;
        private readonly IGameLogger _logger;

        public CraftingPresenter(
            ICraftingSession session,
            ICraftingSlotsView slotsView,
            IPotView potView,
            IArtifactCatalog artifactCatalog,
            IInventoryModeState modeState,
            IGameLogger logger)
        {
            _session = session;
            _slotsView = slotsView;
            _potView = potView;
            _artifactCatalog = artifactCatalog;
            _modeState = modeState;
            _logger = logger;
        }

        public void Initialize()
        {
            _potView.OnBubbleClicked += HandleBubbleClicked;
            _slotsView.OnResultClicked += HandleResultClicked;
            _slotsView.OnStagedItemClicked += HandleStagedItemClicked;
            _slotsView.OnMergeCompleted += HandleMergeCompleted;
            _session.OnItemStaged += HandleItemStaged;
            _session.OnCraftingStarted += HandleCraftingStarted;
            _session.OnCraftSucceeded += HandleCraftSucceeded;
            _session.OnCraftFailed += HandleCraftFailed;
            _session.OnItemUnstaged += HandleItemUnstaged;
            _session.OnResultCollected += HandleResultCollected;
            _session.OnSessionCleared += HandleSessionCleared;
            _modeState.OnModeChanged += HandleModeChanged;
        }

        public void Dispose()
        {
            _potView.OnBubbleClicked -= HandleBubbleClicked;
            _slotsView.OnResultClicked -= HandleResultClicked;
            _slotsView.OnStagedItemClicked -= HandleStagedItemClicked;
            _slotsView.OnMergeCompleted -= HandleMergeCompleted;
            _session.OnItemStaged -= HandleItemStaged;
            _session.OnCraftingStarted -= HandleCraftingStarted;
            _session.OnCraftSucceeded -= HandleCraftSucceeded;
            _session.OnCraftFailed -= HandleCraftFailed;
            _session.OnItemUnstaged -= HandleItemUnstaged;
            _session.OnResultCollected -= HandleResultCollected;
            _session.OnSessionCleared -= HandleSessionCleared;
            _modeState.OnModeChanged -= HandleModeChanged;
        }

        private void HandleModeChanged(InventoryMode mode)
        {
            // Switching away from crafting drops any staged items back into the pot.
            if (mode != InventoryMode.Crafting)
            {
                _session.ReturnAll();
            }
        }

        private void HandleBubbleClicked(int instanceId)
        {
            // Feeding owns clicks in feeding mode; this presenter only crafts.
            if (_modeState.Mode != InventoryMode.Crafting)
            {
                return;
            }

            if (!_session.TrySelect(instanceId))
            {
                _logger.Info(LogCategory.Inventory,$"[CraftingPresenter] Selection of instance {instanceId} rejected.");
            }
        }

        private void HandleResultClicked()
        {
            _session.TryCollectResult();
        }

        private void HandleStagedItemClicked(int instanceId)
        {
            if (!_session.TryUnstage(instanceId))
            {
                _logger.Info(LogCategory.Inventory,$"[CraftingPresenter] Unstaging of instance {instanceId} rejected.");
            }
        }

        private void HandleItemStaged(ArtifactInstance _)
        {
            RefreshStagedItems();
        }

        private void HandleCraftingStarted(IReadOnlyList<ArtifactInstance> _)
        {
            _slotsView.PlayMergeAnimation();
        }

        private void HandleMergeCompleted()
        {
            if (!_session.ResolveCraft())
            {
                _logger.Info(LogCategory.Inventory,"[CraftingPresenter] Craft resolution rejected; the craft was cancelled.");
            }
        }

        private void HandleItemUnstaged(ArtifactInstance _)
        {
            // The rebuild aborts a running merge animation; the unstaged item
            // reappears as a pot bubble via the inventory-added event.
            RefreshStagedItems();
        }

        private void HandleCraftSucceeded(ArtifactInstance result)
        {
            RefreshStagedItems();
            _slotsView.PlaySuccessPuff();
            _slotsView.ShowResult(ToViewData(result));
        }

        private void HandleCraftFailed(ArtifactInstance returned)
        {
            // The view reconciles its own staged list here: the returned item's
            // bubble drops into the pot while the survivors glide back to their
            // slots, so no full staged refresh is wanted.
            _slotsView.PlayFailPuff();
            _slotsView.PlayCraftFailure(returned.InstanceId);
        }

        private void HandleResultCollected(ArtifactInstance _)
        {
            _slotsView.ClearResult(collected: true);
        }

        private void HandleSessionCleared(IReadOnlyList<ArtifactInstance> _)
        {
            RefreshStagedItems();
            _slotsView.ClearResult(collected: false);
        }

        private void RefreshStagedItems()
        {
            var staged = _session.StagedItems;
            var viewData = new List<ArtifactViewData>(staged.Count);
            foreach (var item in staged)
            {
                viewData.Add(ToViewData(item));
            }

            _slotsView.ShowStagedItems(viewData);
        }

        private ArtifactViewData ToViewData(ArtifactInstance instance)
        {
            if (_artifactCatalog.TryGet(instance.DefinitionId, out var definition))
            {
                return new ArtifactViewData(instance.InstanceId, definition.Icon, definition.BubbleTint);
            }

            _logger.Warning(LogCategory.Inventory,$"[CraftingPresenter] No artifact definition for id '{instance.DefinitionId}'.");
            return new ArtifactViewData(instance.InstanceId, null, Color.white);
        }
    }
}
