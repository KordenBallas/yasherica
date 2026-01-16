using Combat.Data;
using Combat.Enemy;
using Combat.Integration;
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

        public CombatIdleState(
            EnemyCombatIntegrator enemyIntegrator,
            IEnemyDataProvider enemyDataProvider)
        {
            _enemyIntegrator = enemyIntegrator;
            _enemyDataProvider = enemyDataProvider;
        }

        public override void OnEnter(IPlatform platform)
        {
            Debug.Log($"[CombatIdleState] Platform {platform.Id} is now idle (combat)");

            // Instantiate enemies FIRST TIME ONLY
            // Content-based detection: check if platform has EnemyContent
            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemyContent && !enemyContent.HasBeenInstantiated)
                {
                    InstantiateEnemy(platform, enemyContent);
                }
            }
        }

        private void InstantiateEnemy(IPlatform platform, EnemyContent enemyContent)
        {
            // Get enemy data
            var enemyData = _enemyDataProvider.GetEnemyData(enemyContent.EnemyId);

            // Create AIPlayer for this enemy
            var enemyPlayer = _enemyIntegrator.CreateEnemyPlayer(enemyContent.EnemyId, enemyData);

            // Load enemy prefab
            GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
            if (enemyPrefab == null)
            {
                Debug.LogError($"[CombatIdleState] Enemy prefab not found at Resources/Prefabs/Enemy");
                return;
            }

            // Instantiate enemy above platform center (will fall via gravity to surface)
            const float spawnHeightOffset = 2f;
            Vector3 spawnPosition = platform.Visual.Position + Vector3.up * spawnHeightOffset;
            GameObject enemyGO = Object.Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemyGO.name = $"Enemy_{enemyContent.EnemyId}";

            // Get or add combat component
            var combatComponent = enemyGO.GetComponent<EnemyCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = enemyGO.AddComponent<EnemyCombatComponent>();
            }

            // Add Rigidbody for gravity simulation if not present
            var rb = enemyGO.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemyGO.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log($"[CombatIdleState] Added Rigidbody to enemy {enemyContent.EnemyId} for gravity simulation");
            }

            // Store in content
            enemyContent.InstantiateEnemy(enemyPlayer, combatComponent);

            Debug.Log($"[CombatIdleState] Enemy {enemyContent.EnemyId} instantiated on platform {platform.Id}");
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[CombatIdleState] Platform {platform.Id} exiting combat idle state");
        }
    }
}
