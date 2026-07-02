using System;
using Combat.Data;
using Core.Logging;
using Loot.Core;
using Loot.Data.Definitions;
using Loot.View;
using Platform;
using UnityEngine;

namespace Loot.Application
{
    public class EnemyLootDropper : IEnemyLootDropper
    {
        private readonly ILootRollService _lootRollService;
        private readonly IEnemyDataProvider _enemyDataProvider;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IWorldArtifactSpawner _spawner;
        private readonly LootConfig _config;
        private readonly IGameLogger _logger;

        public EnemyLootDropper(
            ILootRollService lootRollService,
            IEnemyDataProvider enemyDataProvider,
            ICurrentThemeProvider themeProvider,
            IWorldArtifactSpawner spawner,
            LootConfig config,
            IGameLogger logger)
        {
            _lootRollService = lootRollService;
            _enemyDataProvider = enemyDataProvider;
            _themeProvider = themeProvider;
            _spawner = spawner;
            _config = config;
            _logger = logger;
        }

        public void DropFor(IPlatform platform, bool playerWon)
        {
            if (platform == null || !playerWon)
            {
                return;
            }

            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent
                    && enemyContent.HasBeenInstantiated
                    && !enemyContent.IsEnemyAlive)
                {
                    DropForEnemy(platform, enemyContent);
                }
            }
        }

        private void DropForEnemy(IPlatform platform, EnemyContent enemyContent)
        {
            var enemyData = _enemyDataProvider.GetEnemyData(enemyContent.EnemyId);
            var context = new LootRollContext(
                _themeProvider.CurrentTheme,
                $"enemy:{platform.Id}:{enemyContent.EnemyId}");

            var drops = _lootRollService.RollEnemyDrops(
                enemyData.LootSlots ?? Array.Empty<LootSlotData>(),
                context);
            if (drops.Count == 0)
            {
                return;
            }

            var deathPosition = GetDeathPosition(platform, enemyContent);
            _logger.Info(
                LogCategory.Loot,
                $"[EnemyLootDropper] Enemy {enemyContent.EnemyId} on platform {platform.Id} drops " +
                $"{drops.Count} item(s) [{context.ContextKey}]");

            for (int i = 0; i < drops.Count; i++)
            {
                _spawner.Spawn(drops[i], GetScatteredPosition(deathPosition, i, drops.Count));
            }
        }

        private Vector3 GetDeathPosition(IPlatform platform, EnemyContent enemyContent)
        {
            // The combat component still exists at this point; the platform state
            // change that destroys dead enemies happens after dropping.
            if (enemyContent.EnemyCombatComponent != null)
            {
                return enemyContent.EnemyCombatComponent.transform.position
                    + Vector3.up * _config.SpawnHeightOffset;
            }

            return platform.Visual.Position + Vector3.up * _config.SpawnHeightOffset;
        }

        private Vector3 GetScatteredPosition(Vector3 center, int itemIndex, int itemCount)
        {
            if (itemCount <= 1)
            {
                return center;
            }

            // Deterministic ring layout keeps multiple drops apart without RNG.
            float angle = itemIndex * 2f * Mathf.PI / itemCount;
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _config.SpawnScatterRadius;
        }
    }
}
