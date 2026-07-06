using Combat.Data;
using Combat.Enemy;
using Combat.Integration;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Idle state for platforms with combat content.
    /// Handles enemy instantiation when platform is first loaded.
    /// </summary>
    public class CombatIdleState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<CombatIdleState> { }

        private readonly EnemyCombatIntegrator _enemyIntegrator;
        private readonly IEnemyDataProvider _enemyDataProvider;
        private readonly EnemyVisualSpawner _enemySpawner;
        private readonly Narrative.Interaction.INpcInteractionService _interactionService;
        private readonly IGameLogger _logger;

        public CombatIdleState(
            EnemyCombatIntegrator enemyIntegrator,
            IEnemyDataProvider enemyDataProvider,
            EnemyVisualSpawner enemySpawner,
            Narrative.Interaction.INpcInteractionService interactionService,
            IGameLogger logger)
        {
            _enemyIntegrator = enemyIntegrator;
            _enemyDataProvider = enemyDataProvider;
            _enemySpawner = enemySpawner;
            _interactionService = interactionService;
            _logger = logger;
        }

        public override void OnEnter(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform,$"[CombatIdleState] Platform {platform.Id} is now idle (combat)");

            // A camp crew stands behind its boss: when an NPC (the boss) shares the platform, the
            // enemies carry NO trigger radius of their own — the boss's circle owns the fight.
            bool bossGated = HasNpcContent(platform);

            // Instantiate enemies FIRST TIME ONLY
            // Content-based detection: check if platform has EnemyContent
            int spawnIndex = 0;
            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent && !enemyContent.HasBeenInstantiated)
                {
                    InstantiateEnemy(platform, enemyContent, spawnIndex, bindRadius: !bossGated);
                }

                if (content is EnemyContent)
                {
                    spawnIndex++;
                }
            }
        }

        private void InstantiateEnemy(IPlatform platform, EnemyContent enemyContent, int spawnIndex,
            bool bindRadius)
        {
            // Get enemy data
            var enemyData = _enemyDataProvider.GetEnemyData(enemyContent.EnemyId);

            // Create AIPlayer for this enemy
            var enemyPlayer = _enemyIntegrator.CreateEnemyPlayer(enemyContent.EnemyId, enemyData);

            // Spawn the body via the shared spawner (humanoid assembly first, prefab/capsule fallback)
            var combatComponent = _enemySpawner.Spawn(
                enemyData, enemyContent.EnemyId, platform.Visual.Position, spawnIndex);
            if (combatComponent == null)
            {
                return;
            }

            // Store in content
            enemyContent.InstantiateEnemy(enemyPlayer, combatComponent);

            // A lone monster is approached, not landed on: its aggro radius is the fight's trigger
            // (name + `!` overhead, platform-scoped — the NPC proximity model).
            if (bindRadius)
            {
                _interactionService?.BindEnemy(enemyContent, platform, enemyData?.Name);
            }

            _logger.Info(LogCategory.Platform,$"[CombatIdleState] Enemy {enemyContent.EnemyId} instantiated on platform {platform.Id}");
        }

        private static bool HasNpcContent(IPlatform platform)
        {
            foreach (var content in platform.Contents)
            {
                if (content is NpcContent)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnExit(IPlatform platform)
        {
            _logger.Info(LogCategory.Platform,$"[CombatIdleState] Platform {platform.Id} exiting combat idle state");
        }
    }
}
