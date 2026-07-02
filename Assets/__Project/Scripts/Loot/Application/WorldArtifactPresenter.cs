using System;
using Character;
using Core.Logging;
using Inventory.Core;
using Loot.Core;
using Loot.View;
using UnityEngine;

namespace Loot.Application
{
    /// <summary>
    /// Drives one world artifact pickup: filters trigger contacts down to the
    /// player, checks capacity, runs the pickup animation, then adds the
    /// artifact(s) to the inventory and removes the world entity.
    /// </summary>
    public class WorldArtifactPresenter : IDisposable
    {
        private readonly IWorldArtifactView _view;
        private readonly LootRollResult _loot;
        private readonly IInventoryModel _inventory;
        private readonly IInventoryCapacityPolicy _capacityPolicy;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly IGameLogger _logger;
        private readonly Action _onCollected;

        private bool _pickupStarted;

        public WorldArtifactPresenter(
            IWorldArtifactView view,
            LootRollResult loot,
            IInventoryModel inventory,
            IInventoryCapacityPolicy capacityPolicy,
            ICharacterRegistry characterRegistry,
            IGameLogger logger,
            Action onCollected)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _capacityPolicy = capacityPolicy ?? throw new ArgumentNullException(nameof(capacityPolicy));
            _characterRegistry = characterRegistry ?? throw new ArgumentNullException(nameof(characterRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onCollected = onCollected;

            _view.OnBodyEntered += HandleBodyEntered;
            _view.OnViewDestroyed += Dispose;
        }

        public void Dispose()
        {
            _view.OnBodyEntered -= HandleBodyEntered;
            _view.OnViewDestroyed -= Dispose;
        }

        private void HandleBodyEntered(Transform body)
        {
            if (_pickupStarted || !IsPlayer(body))
            {
                return;
            }

            _view.SetInteractable(false);

            if (!_capacityPolicy.CanAccept(_loot.ArtifactId))
            {
                // Unreachable while the inventory is unlimited; kept as the hook
                // for a future bounded inventory.
                _logger.Info(LogCategory.Loot, $"[WorldArtifactPresenter] Inventory cannot accept '{_loot.ArtifactId}'");
                _view.PlayRejectFeedback();
                _view.SetInteractable(true);
                return;
            }

            _pickupStarted = true;
            var player = _characterRegistry.GetPlayerCharacter();
            _view.PlayPickupAnimation(player, CompletePickup);
        }

        private bool IsPlayer(Transform body)
        {
            var player = _characterRegistry.GetPlayerCharacter();
            return player != null && body != null && body.IsChildOf(player);
        }

        private void CompletePickup()
        {
            for (int i = 0; i < _loot.Quantity; i++)
            {
                _inventory.Add(_loot.ArtifactId);
            }

            _logger.Info(LogCategory.Loot, $"[WorldArtifactPresenter] Picked up {_loot.Quantity}x '{_loot.ArtifactId}'");
            _onCollected?.Invoke();
            _view.DestroySelf();
        }
    }
}
