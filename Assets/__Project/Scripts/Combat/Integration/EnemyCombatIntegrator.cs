using System.Collections;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Combat.Data;
using Combat.Data.Definitions;
using Combat.Data.Providers;
using Combat.Enemy;
using Combat.Player;
using UnityEngine;
using Zenject;

namespace Combat.Integration
{
    /// <summary>
    /// Service for integrating enemy units into combat.
    /// Mirrors CharacterCombatInitializer pattern for enemies.
    /// Single responsibility: enemy combat setup and integration.
    /// Pure C# service with constructor injection.
    /// </summary>
    public class EnemyCombatIntegrator
    {
        private readonly CombatEntryAnimator _entryAnimator;
        private readonly IEnemyDataProvider _enemyDataProvider;
        private readonly DiContainer _container;

        private static int _nextPlayerId = 100;  // Start enemy player IDs at 100
        private static int _nextUnitId = 2000;   // Start enemy unit IDs at 2000

        public EnemyCombatIntegrator(
            CombatEntryAnimator entryAnimator,
            IEnemyDataProvider enemyDataProvider,
            DiContainer container)
        {
            _entryAnimator = entryAnimator;
            _enemyDataProvider = enemyDataProvider;
            _container = container;
        }

        /// <summary>
        /// Creates AIPlayer for enemy with appropriate decision maker based on AI personality.
        /// Uses ConfigurableTacticalAI when AIProfileDefinition is available.
        /// </summary>
        public IPlayer CreateEnemyPlayer(int enemyId, EnemyData enemyData)
        {
            // Try to get AIProfile from ScriptableObject provider
            AIProfileDefinition aiProfile = null;
            if (_enemyDataProvider is ScriptableObjectEnemyDataProvider soProvider)
            {
                var enemyDefinition = soProvider.GetEnemyDefinition(enemyId);
                aiProfile = enemyDefinition?.AIProfile;
            }

            IAIDecisionMaker decisionMaker = CreateDecisionMaker(enemyData.AIType, aiProfile);

            var playerId = _nextPlayerId++;
            var player = new AIPlayer(
                id: playerId,
                name: enemyData.Name,
                decisionMaker: decisionMaker);

            string aiDescription = aiProfile != null
                ? $"{enemyData.AIType} (Configurable)"
                : enemyData.AIType.ToString();
            Debug.Log($"[EnemyCombatIntegrator] Created AIPlayer: ID={playerId}, Name={enemyData.Name}, AI={aiDescription}");

            return player;
        }

        /// <summary>
        /// Creates the appropriate AI decision maker based on personality and profile.
        /// </summary>
        private IAIDecisionMaker CreateDecisionMaker(AIPersonality personality, AIProfileDefinition profile)
        {
            // If we have an AI profile, use ConfigurableTacticalAI for Tactical personality
            if (profile != null && personality == AIPersonality.Tactical)
            {
                return new ConfigurableTacticalAI(profile);
            }

            // Fallback to standard AI implementations
            return personality switch
            {
                AIPersonality.SimpleRandom => new SimpleRandomAI(),
                AIPersonality.Tactical => new TacticalAI(),
                _ => new SimpleRandomAI()
            };
        }

        /// <summary>
        /// Integrates enemy into combat system.
        /// Similar to CharacterCombatInitializer.InitializeCharacterForCombat
        /// Repositions existing enemy GameObject to battlefield cell and initializes for combat.
        /// </summary>
        public IEnumerator IntegrateEnemyForCombat(
            int enemyId,
            IPlayer enemyPlayer,
            EnemyCombatComponent existingComponent,
            IBattlefield battlefield,
            ICombatController combatController,
            Vector3 platformCenter)
        {
            Debug.Log($"[EnemyCombatIntegrator] Starting integration for enemy {enemyId}");

            // Get enemy data
            EnemyData enemyData = _enemyDataProvider.GetEnemyData(enemyId);

            // Find closest battlefield cell to platform center
            HexCoordinates startCell = _entryAnimator.FindClosestCell(platformCenter, battlefield);
            Debug.Log($"[EnemyCombatIntegrator] Found closest cell for enemy: {startCell}");

            // Reposition existing enemy GameObject to battlefield cell
            Vector3 worldPosition = battlefield.HexToWorld(startCell);
            existingComponent.transform.position = worldPosition;
            Debug.Log($"[EnemyCombatIntegrator] Repositioned enemy {enemyId} to {worldPosition}");

            // Make Rigidbody kinematic during combat to prevent physics interference
            var rb = existingComponent.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                Debug.Log($"[EnemyCombatIntegrator] Set enemy {enemyId} Rigidbody to kinematic for combat");
            }

            // Initialize component for combat
            int unitId = _nextUnitId++;
            existingComponent.InitializeForCombat(unitId, enemyPlayer, startCell, combatController, enemyData);
            Debug.Log($"[EnemyCombatIntegrator] Initialized EnemyCombatComponent for combat");

            // Add internal Unit to combat state (NOT the MonoBehaviour component)
            combatController.AddUnit(existingComponent.InternalUnit);
            Debug.Log($"[EnemyCombatIntegrator] Enemy {enemyId} integrated: UnitID={unitId}, Cell={startCell}, Position={worldPosition}");

            yield return null;
        }
    }
}
