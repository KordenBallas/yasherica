using System;
using System.Collections.Generic;
using Combat.Integration;
using Core.Camera;
using Core.Logging;
using Character;
using Inventory.Core;
using Inventory.Data;
using Inventory.Data.Definitions;
using Inventory.View;
using UnityEngine;
using Zenject;

namespace Inventory.Presenter
{
    /// <summary>
    /// Orchestrates the cauldron inventory: open/close flow (camera zoom, character
    /// facing, movement lock, stage overlay, HUD buttons), bubble layout refresh on
    /// inventory changes, starting-inventory seeding, and the combat guard
    /// (inventory is unavailable during combat).
    /// </summary>
    public class InventoryPresenter : IInitializable, IDisposable
    {
        private readonly IInventoryHudView _hudView;
        private readonly IPotView _potView;
        private readonly IInventoryStageView _stageView;
        private readonly ICameraService _cameraService;
        private readonly IBellyAnchorProvider _bellyAnchorProvider;
        private readonly ICharacterFacing _characterFacing;
        private readonly IMovementInputLock _movementInputLock;
        private readonly IInventoryModel _inventory;
        private readonly ICraftingSession _craftingSession;
        private readonly IFeedingSession _feedingSession;
        private readonly IInventoryModeState _modeState;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly ICombatActivityTracker _combatActivityTracker;
        private readonly InventoryConfig _config;
        private readonly BubbleLayoutCalculator _layoutCalculator;
        private readonly IGameLogger _logger;

        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public InventoryPresenter(
            IInventoryHudView hudView,
            IPotView potView,
            IInventoryStageView stageView,
            ICameraService cameraService,
            IBellyAnchorProvider bellyAnchorProvider,
            ICharacterFacing characterFacing,
            IMovementInputLock movementInputLock,
            IInventoryModel inventory,
            ICraftingSession craftingSession,
            IFeedingSession feedingSession,
            IInventoryModeState modeState,
            IArtifactCatalog artifactCatalog,
            ICombatActivityTracker combatActivityTracker,
            InventoryConfig config,
            BubbleLayoutCalculator layoutCalculator,
            IGameLogger logger)
        {
            _hudView = hudView;
            _potView = potView;
            _stageView = stageView;
            _cameraService = cameraService;
            _bellyAnchorProvider = bellyAnchorProvider;
            _characterFacing = characterFacing;
            _movementInputLock = movementInputLock;
            _inventory = inventory;
            _craftingSession = craftingSession;
            _feedingSession = feedingSession;
            _modeState = modeState;
            _artifactCatalog = artifactCatalog;
            _combatActivityTracker = combatActivityTracker;
            _config = config;
            _layoutCalculator = layoutCalculator;
            _logger = logger;
        }

        public void Initialize()
        {
            _hudView.OnOpenClicked += HandleOpenClicked;
            _hudView.OnCloseClicked += HandleCloseClicked;
            _hudView.OnFeedModeToggled += HandleFeedModeToggled;
            _modeState.OnModeChanged += HandleModeChanged;
            _inventory.OnItemAdded += HandleInventoryChanged;
            _inventory.OnItemRemoved += HandleInventoryChanged;
            _combatActivityTracker.OnCombatActivityChanged += HandleCombatActivityChanged;

            _hudView.SetOpenButtonVisible(true);
            _hudView.SetOpenButtonInteractable(!_combatActivityTracker.IsCombatActive);
            _hudView.SetCloseButtonVisible(false);
            _hudView.SetFeedModeToggleVisible(false);
            _hudView.SetFeedModeActive(false);
            _potView.SetPotFocused(false);

            SeedStartingInventory();
        }

        public void Dispose()
        {
            _hudView.OnOpenClicked -= HandleOpenClicked;
            _hudView.OnCloseClicked -= HandleCloseClicked;
            _hudView.OnFeedModeToggled -= HandleFeedModeToggled;
            _modeState.OnModeChanged -= HandleModeChanged;
            _inventory.OnItemAdded -= HandleInventoryChanged;
            _inventory.OnItemRemoved -= HandleInventoryChanged;
            _combatActivityTracker.OnCombatActivityChanged -= HandleCombatActivityChanged;
        }

        private void HandleOpenClicked()
        {
            if (_isOpen || _cameraService.IsTransitioning)
            {
                return;
            }

            if (_combatActivityTracker.IsCombatActive)
            {
                _logger.Info("[InventoryPresenter] Inventory is unavailable during combat.");
                return;
            }

            _isOpen = true;
            _movementInputLock.SetMovementEnabled(false);
            _characterFacing.FaceTowards(_cameraService.OutputCameraPosition, _config.FacingRotationDuration);
            _cameraService.SetBellyAnchor(_bellyAnchorProvider.BellyAnchor);
            _cameraService.SwitchToBellyCamera(_config.CameraTransitionTime);
            _stageView.SetStageActive(true);

            RefreshBubbles();
            _potView.SetPotFocused(true);
            _hudView.SetOpenButtonVisible(false);
            _hudView.SetCloseButtonVisible(true);
            // The pot opens in crafting mode; the feed toggle switches into feeding.
            _modeState.SetMode(InventoryMode.Crafting);
            _hudView.SetFeedModeToggleVisible(true);
            _hudView.SetFeedModeActive(false);
        }

        private void HandleCloseClicked()
        {
            if (!_isOpen)
            {
                return;
            }

            Close();
        }

        private void Close()
        {
            _isOpen = false;

            // Anything left above the pot falls back into it when the belly view closes.
            _craftingSession.ReturnAll();
            _feedingSession.ReturnAll();
            // Reset to crafting so the next open starts in the default mode.
            _modeState.SetMode(InventoryMode.Crafting);

            _potView.SetPotFocused(false);
            _stageView.SetStageActive(false);
            _cameraService.SwitchToPreviousCamera(_config.CameraTransitionTime);
            _characterFacing.RestoreFacing(_config.FacingRotationDuration);
            _movementInputLock.SetMovementEnabled(true);
            _hudView.SetOpenButtonVisible(true);
            _hudView.SetOpenButtonInteractable(!_combatActivityTracker.IsCombatActive);
            _hudView.SetCloseButtonVisible(false);
            _hudView.SetFeedModeToggleVisible(false);
            _hudView.SetFeedModeActive(false);
        }

        private void HandleFeedModeToggled()
        {
            if (!_isOpen)
            {
                return;
            }

            var nextMode = _modeState.Mode == InventoryMode.Feeding
                ? InventoryMode.Crafting
                : InventoryMode.Feeding;
            _modeState.SetMode(nextMode);
            _hudView.SetFeedModeActive(nextMode == InventoryMode.Feeding);
        }

        private void HandleModeChanged(InventoryMode mode)
        {
            // Feeding drops the stage camera to reveal the slots below the pot.
            _stageView.SetFeedingFraming(mode == InventoryMode.Feeding);
        }

        private void HandleCombatActivityChanged(bool isCombatActive)
        {
            _hudView.SetOpenButtonInteractable(!isCombatActive);

            if (isCombatActive && _isOpen)
            {
                _logger.Info("[InventoryPresenter] Combat started - closing inventory.");
                Close();
            }
        }

        private void HandleInventoryChanged(ArtifactInstance _)
        {
            if (_isOpen)
            {
                RefreshBubbles();
            }
        }

        private void RefreshBubbles()
        {
            var items = _inventory.Items;
            var halfExtents = _potView.PotInteriorHalfExtents;
            var settings = new BubbleLayoutSettings(
                _config.MinBubbleRadius,
                _config.MaxBubbleRadius,
                _config.RadiusFalloff,
                _config.EdgePadding);

            var placements = _layoutCalculator.Calculate(
                items.Count,
                halfExtents.x,
                halfExtents.y,
                _potView.PotInteriorHalfDepth,
                settings);

            var bubbles = new List<BubbleViewData>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ResolveVisual(items[i].DefinitionId, out Sprite icon, out Color tint);
                bubbles.Add(new BubbleViewData(items[i].InstanceId, icon, tint, placements[i]));
            }

            _potView.ShowBubbles(bubbles);
        }

        private void ResolveVisual(string definitionId, out Sprite icon, out Color tint)
        {
            if (_artifactCatalog.TryGet(definitionId, out var definition))
            {
                icon = definition.Icon;
                tint = definition.BubbleTint;
                return;
            }

            _logger.Warning($"[InventoryPresenter] No artifact definition for id '{definitionId}'.");
            icon = null;
            tint = Color.white;
        }

        private void SeedStartingInventory()
        {
            var startingItems = _config.StartingInventory;
            if (startingItems == null)
            {
                return;
            }

            foreach (var definition in startingItems)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    _logger.Warning("[InventoryPresenter] Skipping invalid starting inventory entry.");
                    continue;
                }

                _inventory.Add(definition.Id);
            }
        }
    }
}
