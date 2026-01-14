using Combat.Data;
using Combat.Enemy;
using Combat.Integration;
using UnityEngine;

namespace Platform
{
    public class PlatformIdleState : PlatformStateBase
    {
        private readonly EnemyCombatIntegrator _enemyIntegrator;
        private readonly IEnemyDataProvider _enemyDataProvider;

        /// <summary>
        /// Constructor for dependency injection.
        /// Dependencies are optional to support platforms without enemy combat systems.
        /// </summary>
        public PlatformIdleState(
            EnemyCombatIntegrator enemyIntegrator = null,
            IEnemyDataProvider enemyDataProvider = null)
        {
            _enemyIntegrator = enemyIntegrator;
            _enemyDataProvider = enemyDataProvider;
        }

        public override void OnEnter(IPlatform platform)
        {
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} is now idle");

            // Instantiate enemies FIRST TIME ONLY
            if (_enemyIntegrator != null && _enemyDataProvider != null && platform is CombatPlatform)
            {
                foreach (var content in platform.Contents)
                {
                    if (content is EnemyContent enemyContent && !enemyContent.HasBeenInstantiated)
                    {
                        // Get enemy data
                        var enemyData = _enemyDataProvider.GetEnemyData(enemyContent.EnemyId);

                        // Create AIPlayer for this enemy
                        var enemyPlayer = _enemyIntegrator.CreateEnemyPlayer(enemyContent.EnemyId, enemyData);

                        // Load enemy prefab
                        GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
                        if (enemyPrefab == null)
                        {
                            Debug.LogError($"[PlatformIdleState] Enemy prefab not found at Resources/Prefabs/Enemy");
                            continue;
                        }

                        // Instantiate enemy above platform center (will fall via gravity to surface)
                        const float spawnHeightOffset = 2f;  // Height above platform, similar to character dashHeightOffset
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
                            rb.constraints = RigidbodyConstraints.FreezeRotation;  // Prevent enemy from tipping over
                            Debug.Log($"[PlatformIdleState] Added Rigidbody to enemy {enemyContent.EnemyId} for gravity simulation");
                        }

                        // Store in content
                        enemyContent.InstantiateEnemy(enemyPlayer, combatComponent);

                        Debug.Log($"[PlatformIdleState] Enemy {enemyContent.EnemyId} instantiated on platform {platform.Id}");
                    }
                }
            }
        }

        public override void OnExit(IPlatform platform)
        {
            Debug.Log($"[PlatformIdleState] Platform {platform.Id} exiting idle state");
        }
    }
}

