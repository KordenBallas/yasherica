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
    /// facing, movement lock, stage overlay, HUD buttons), stable-spot bubble
    /// refresh on inventory changes, the fullness fill-level snapshot on open
    /// (Track F), starting-inventory seeding, and the combat guard (inventory is
    /// unavailable during combat).
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
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly ICombatActivityTracker _combatActivityTracker;
        private readonly InventoryConfig _config;
        private readonly BrewLayoutModel _brewLayout;
        private readonly ILiquidSurfaceView _liquidView;
        private readonly IGameLogger _logger;
        private readonly global::Core.Persistence.RunRestoreContext _restoreContext;

        // Spreads neighbouring bubbles' idle bob out of phase; spot indices are
        // sequential, so a non-integer step decorrelates them.
        private const float BobPhaseStep = 0.9f;

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
            IArtifactCatalog artifactCatalog,
            ICombatActivityTracker combatActivityTracker,
            InventoryConfig config,
            BrewLayoutModel brewLayout,
            ILiquidSurfaceView liquidView,
            IGameLogger logger,
            [Zenject.InjectOptional] global::Core.Persistence.RunRestoreContext restoreContext = null)
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
            _artifactCatalog = artifactCatalog;
            _combatActivityTracker = combatActivityTracker;
            _config = config;
            _brewLayout = brewLayout;
            _liquidView = liquidView;
            _logger = logger;
            _restoreContext = restoreContext;
        }

        public void Initialize()
        {
            _hudView.OnOpenClicked += HandleOpenClicked;
            _hudView.OnCloseClicked += HandleCloseClicked;
            _inventory.OnItemAdded += HandleInventoryChanged;
            _inventory.OnItemRemoved += HandleInventoryChanged;
            _combatActivityTracker.OnCombatActivityChanged += HandleCombatActivityChanged;

            _hudView.SetOpenButtonVisible(true);
            _hudView.SetOpenButtonInteractable(!_combatActivityTracker.IsCombatActive);
            _hudView.SetCloseButtonVisible(false);
            _potView.SetPotFocused(false);

            SeedStartingInventory();
        }

        public void Dispose()
        {
            _hudView.OnOpenClicked -= HandleOpenClicked;
            _hudView.OnCloseClicked -= HandleCloseClicked;
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
                _logger.Info(LogCategory.Inventory,"[InventoryPresenter] Inventory is unavailable during combat.");
                return;
            }

            _isOpen = true;
            _movementInputLock.SetMovementEnabled(false);
            _characterFacing.FaceTowards(_cameraService.OutputCameraPosition, _config.FacingRotationDuration);
            _cameraService.SetBellyAnchor(_bellyAnchorProvider.BellyAnchor);
            _cameraService.SwitchToBellyCamera(_config.CameraTransitionTime);
            _stageView.SetStageActive(true);

            // The fill level snapshots on open and holds for the session (F2):
            // it reads the just-synced spot stack so the waterline always sits
            // above the topmost bubble.
            RefreshBubbles();
            SnapshotFillLevel();
            _potView.SetPotFocused(true);
            _hudView.SetOpenButtonVisible(false);
            _hudView.SetCloseButtonVisible(true);
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

            // Anything left above the pot falls back into it when the belly view
            // closes. Blank sockets deliberately keep their contents - incubation
            // persists across open/close (mutation-subsystem.md §2.3).
            _craftingSession.ReturnAll();

            _potView.SetPotFocused(false);
            _stageView.SetStageActive(false);
            _cameraService.SwitchToPreviousCamera(_config.CameraTransitionTime);
            _characterFacing.RestoreFacing(_config.FacingRotationDuration);
            _movementInputLock.SetMovementEnabled(true);
            _hudView.SetOpenButtonVisible(true);
            _hudView.SetOpenButtonInteractable(!_combatActivityTracker.IsCombatActive);
            _hudView.SetCloseButtonVisible(false);
        }

        private void HandleCombatActivityChanged(bool isCombatActive)
        {
            _hudView.SetOpenButtonInteractable(!isCombatActive);

            if (isCombatActive && _isOpen)
            {
                _logger.Info(LogCategory.Inventory,"[InventoryPresenter] Combat started - closing inventory.");
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
            var ids = new List<int>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ids.Add(items[i].InstanceId);
            }

            _brewLayout.Sync(ids);

            var bubbles = new List<BubbleViewData>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                if (!_brewLayout.TryGetSpot(items[i].InstanceId, out var spot))
                {
                    // Lattice exhausted (extreme overfill): the artifact stays in
                    // the inventory but gets no bubble this session.
                    _logger.Warning(LogCategory.Inventory,
                        $"[InventoryPresenter] No free brew spot for instance {items[i].InstanceId}; " +
                        $"lattice capacity is {_brewLayout.Capacity}.");
                    continue;
                }

                ResolveVisual(items[i].DefinitionId, out Sprite icon, out Color tint);
                var placement = new BubblePlacement(
                    spot.X, spot.Y, spot.Z, _config.BrewBubbleRadius, spot.Index * BobPhaseStep);
                bubbles.Add(new BubbleViewData(items[i].InstanceId, icon, tint, placement));
            }

            _potView.ShowBubbles(bubbles);
        }

        private void SnapshotFillLevel()
        {
            float highestBubbleTop = _inventory.Items.Count > 0
                ? _brewLayout.HighestOccupiedY + _config.BrewBubbleRadius
                : 0f;
            float fillHeight = LiquidFillCalculator.Calculate(
                _inventory.Items.Count,
                highestBubbleTop,
                new LiquidFillSettings(
                    _config.LiquidMinFillHeight,
                    _config.LiquidMaxFillHeight,
                    _config.ArtifactsAtFullPot,
                    _config.LiquidFillHeadroom));
            _liquidView.SetFillHeight(fillHeight);
        }

        private void ResolveVisual(string definitionId, out Sprite icon, out Color tint)
        {
            if (_artifactCatalog.TryGet(definitionId, out var definition))
            {
                icon = definition.Icon;
                tint = definition.BubbleTint;
                return;
            }

            _logger.Warning(LogCategory.Inventory,$"[InventoryPresenter] No artifact definition for id '{definitionId}'.");
            icon = null;
            tint = Color.white;
        }

        private void SeedStartingInventory()
        {
            // A continued run's inventory is savepoint-true — re-seeding the dev items there would
            // duplicate them on every load (P2-2).
            if (_restoreContext != null && _restoreContext.IsRestoring)
            {
                return;
            }

            // Dev seed only while the inventory is untouched, so a later re-initialize never
            // duplicates items (mirrors the blank-rack guard).
            if (_inventory.Items.Count > 0)
            {
                return;
            }

            var startingItems = _config.StartingInventory;
            if (startingItems == null)
            {
                return;
            }

            foreach (var definition in startingItems)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    _logger.Warning(LogCategory.Inventory,"[InventoryPresenter] Skipping invalid starting inventory entry.");
                    continue;
                }

                _inventory.Add(definition.Id);
            }
        }
    }
}
