using System;
using Character;
using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Loot.Application;
using Loot.Core;
using Loot.Data.Definitions;
using UnityEngine;

namespace Loot.View
{
    /// <summary>
    /// Instantiates a pickup view via the Zenject factory, configures its visuals
    /// from the artifact definition and wires a presenter to it.
    /// </summary>
    public class WorldArtifactSpawner : IWorldArtifactSpawner
    {
        private readonly WorldArtifactView.Factory _viewFactory;
        private readonly IArtifactCatalog _artifactCatalog;
        private readonly LootConfig _config;
        private readonly IInventoryModel _inventory;
        private readonly IInventoryCapacityPolicy _capacityPolicy;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly IGameLogger _logger;

        public WorldArtifactSpawner(
            WorldArtifactView.Factory viewFactory,
            IArtifactCatalog artifactCatalog,
            LootConfig config,
            IInventoryModel inventory,
            IInventoryCapacityPolicy capacityPolicy,
            ICharacterRegistry characterRegistry,
            IGameLogger logger)
        {
            _viewFactory = viewFactory;
            _artifactCatalog = artifactCatalog;
            _config = config;
            _inventory = inventory;
            _capacityPolicy = capacityPolicy;
            _characterRegistry = characterRegistry;
            _logger = logger;
        }

        public void Spawn(LootRollResult loot, Vector3 position, Action onCollected = null)
        {
            if (!_artifactCatalog.TryGet(loot.ArtifactId, out var definition))
            {
                _logger.Warning(
                    LogCategory.Loot,
                    $"[WorldArtifactSpawner] Unknown artifact id '{loot.ArtifactId}' - pickup skipped");
                return;
            }

            var view = _viewFactory.Create();
            view.transform.position = position;
            view.Configure(definition.Icon, _config);

            // The presenter disposes itself when the view is destroyed.
            var presenter = new WorldArtifactPresenter(
                view, loot, _inventory, _capacityPolicy, _characterRegistry, _logger, onCollected);

            _logger.Info(LogCategory.Loot, $"[WorldArtifactSpawner] Spawned '{loot.ArtifactId}' x{loot.Quantity} at {position}");
        }
    }
}
