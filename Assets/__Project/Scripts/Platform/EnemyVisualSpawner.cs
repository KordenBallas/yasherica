using CharacterSystem.Runtime;
using Combat.Data;
using Combat.Enemy;
using Core.Logging;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Builds an enemy's world GameObject — the single spawn path for both platform combat states
    /// (previously two duplicated bodies). Assembly-first: the shared modular humanoid, tinted by the
    /// enemy's demo role colour, so a bandit reads as the same humanoid in the world and in combat.
    /// An enemy without an authored assembly gets the DEFAULT humanoid assembly (capsules do not
    /// exist — PO decision 2026-07-05); the authored prefab is only reached when the factory itself
    /// fails, and the legacy capsule only when even that is absent (logged error).
    /// </summary>
    public sealed class EnemyVisualSpawner
    {
        private const string FallbackPrefabPath = "Prefabs/Enemy";
        private const string DefaultAssemblyPath = "CharacterSystem/Assemblies/PlaceholderAssembly_A";

        // Enemies without an authored tint still read as enemies: the shared demo enemy red
        // (the camp crew's lighter maroon).
        private static readonly Color DefaultEnemyTint = new Color(0.55f, 0.2f, 0.25f, 1f);

        // Prefab bodies carry a collider and drop onto the surface via gravity; assembled rigs have
        // no collider, so they are placed feet-on-surface directly (same as NPC visuals).
        private const float PrefabSpawnHeightOffset = 2f;

        // Deterministic ring around the platform centre so a boss-and-crew group reads as a camp,
        // not a stack: first enemy at the centre, the rest spaced on the ring.
        private const float CrewRingRadius = 1.5f;
        private const float CrewRingAngleStepDegrees = 72f;

        private readonly IModularCharacterFactory _modularFactory;
        private readonly IDemoRoleTintApplier _tintApplier;
        private readonly IGameLogger _logger;

        public EnemyVisualSpawner(
            IModularCharacterFactory modularFactory,
            IDemoRoleTintApplier tintApplier,
            IGameLogger logger)
        {
            _modularFactory = modularFactory;
            _tintApplier = tintApplier;
            _logger = logger;
        }

        /// <summary>
        /// Spawns the enemy body and returns its combat component, or null when no visual source
        /// could be resolved. <paramref name="spawnIndex"/> is the enemy's position among the
        /// platform's enemies (0 = centre, N = ring slot N) — pass the loop index.
        /// </summary>
        public EnemyCombatComponent Spawn(EnemyData enemyData, int enemyId, Vector3 platformCenter, int spawnIndex)
        {
            Vector3 basePosition = platformCenter + RingOffset(spawnIndex);
            GameObject enemyGO = SpawnBody(enemyData, enemyId, basePosition);
            if (enemyGO == null)
            {
                return null;
            }

            enemyGO.name = spawnIndex == 0 ? $"Enemy_{enemyId}" : $"Enemy_{enemyId}_{spawnIndex}";

            var tint = enemyData != null && DemoRoleTintApplier.ShouldTint(enemyData.DemoTint)
                ? enemyData.DemoTint
                : DefaultEnemyTint;
            _tintApplier?.Apply(enemyGO, tint);

            var combatComponent = enemyGO.GetComponent<EnemyCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = enemyGO.AddComponent<EnemyCombatComponent>();
            }

            return combatComponent;
        }

        private GameObject SpawnBody(EnemyData enemyData, int enemyId, Vector3 basePosition)
        {
            // No authored assembly → the default shared humanoid: every enemy is a humanoid.
            var assembly = enemyData?.Assembly;
            if (assembly == null)
            {
                assembly = Resources.Load<CharacterSystem.Data.Definitions.CharacterAssemblyDefinition>(
                    DefaultAssemblyPath);
            }

            if (assembly != null && _modularFactory != null)
            {
                var character = _modularFactory.Create(assembly, null);
                if (character != null)
                {
                    // Host the rig so the enemy root's +Z is the model's face (the placeholder
                    // art faces -Z) — combat facing/locomotion yaw the root and must read face-first.
                    return CharacterRigHost.Wrap(character, $"Enemy_{enemyId}", basePosition);
                }

                _logger?.Warning(LogCategory.Platform,
                    $"[EnemyVisualSpawner] Assembly for enemy {enemyId} failed to build — falling back to prefab.");
            }

            GameObject prefab = enemyData?.Prefab;
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(FallbackPrefabPath);
                if (prefab == null)
                {
                    _logger?.Error(LogCategory.Platform,
                        $"[EnemyVisualSpawner] No assembly, prefab, or fallback capsule for enemy {enemyId} — no visual.");
                    return null;
                }

                _logger?.Warning(LogCategory.Platform,
                    $"[EnemyVisualSpawner] Enemy {enemyId} has no assembly or prefab — using the legacy capsule.");
            }

            Vector3 spawnPosition = basePosition + Vector3.up * PrefabSpawnHeightOffset;
            GameObject enemyGO = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);

            // Prefab bodies fall onto the surface; assembled rigs never take this path.
            var rb = enemyGO.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemyGO.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            return enemyGO;
        }

        private static Vector3 RingOffset(int spawnIndex)
        {
            if (spawnIndex <= 0)
            {
                return Vector3.zero;
            }

            float angle = spawnIndex * CrewRingAngleStepDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * CrewRingRadius;
        }
    }
}
