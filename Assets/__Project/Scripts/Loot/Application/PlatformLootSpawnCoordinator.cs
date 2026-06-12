using System;
using System.Collections.Generic;
using Core.Events;
using Loot.Data.Definitions;
using Loot.View;
using Platform;
using UnityEngine;
using Zenject;

namespace Loot.Application
{
    /// <summary>
    /// Spawns world pickups for platform discovery loot when a platform is
    /// entered. Collected flags on LootContent prevent respawning; an internal
    /// set prevents duplicate spawns while the pickups are still in the world.
    /// </summary>
    public class PlatformLootSpawnCoordinator : IInitializable, IDisposable
    {
        private readonly IWorldArtifactSpawner _spawner;
        private readonly LootConfig _config;
        private readonly HashSet<int> _spawnedPlatformIds = new HashSet<int>();

        public PlatformLootSpawnCoordinator(IWorldArtifactSpawner spawner, LootConfig config)
        {
            _spawner = spawner;
            _config = config;
        }

        public void Initialize()
        {
            PlatformEvents.OnPlatformEntered += HandlePlatformEntered;
        }

        public void Dispose()
        {
            PlatformEvents.OnPlatformEntered -= HandlePlatformEntered;
        }

        private void HandlePlatformEntered(IPlatform platform)
        {
            if (platform?.Visual == null || !_spawnedPlatformIds.Add(platform.Id))
            {
                return;
            }

            foreach (var content in platform.Contents)
            {
                if (content is LootContent lootContent)
                {
                    SpawnLoot(platform, lootContent);
                }
            }
        }

        private void SpawnLoot(IPlatform platform, LootContent lootContent)
        {
            int count = lootContent.Items.Count;
            for (int i = 0; i < count; i++)
            {
                if (lootContent.IsCollected(i))
                {
                    continue;
                }

                int itemIndex = i;
                _spawner.Spawn(
                    lootContent.Items[i],
                    GetSpawnPosition(platform, i, count),
                    onCollected: () => lootContent.MarkCollected(itemIndex));
            }
        }

        private Vector3 GetSpawnPosition(IPlatform platform, int itemIndex, int itemCount)
        {
            var position = platform.Visual.Position + Vector3.up * _config.SpawnHeightOffset;

            // Deterministic ring layout keeps multiple pickups apart without RNG.
            if (itemCount > 1)
            {
                float angle = itemIndex * 2f * Mathf.PI / itemCount;
                position += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _config.SpawnScatterRadius;
            }

            return position;
        }
    }
}
