using System.Collections;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Data;
using Combat.Data.Definitions;
using Combat.Data.Providers;
using Combat.Enemy;
using Combat.Player;
using Core.Logging;
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
        private readonly HexDirectionConfig _hexDirectionConfig;
        private readonly Loot.Core.IRunSeedProvider _runSeedProvider;
        private readonly View.ICombatUnitViewRegistry _unitViewRegistry;
        private readonly DiContainer _container;
        private readonly IGameLogger _logger;

        private const string AiSeedContext = "combat-ai";

        private static int _nextPlayerId = 100;  // Start enemy player IDs at 100
        private static int _nextUnitId = 2000;   // Start enemy unit IDs at 2000

        public EnemyCombatIntegrator(
            CombatEntryAnimator entryAnimator,
            IEnemyDataProvider enemyDataProvider,
            HexDirectionConfig hexDirectionConfig,
            Loot.Core.IRunSeedProvider runSeedProvider,
            View.ICombatUnitViewRegistry unitViewRegistry,
            DiContainer container,
            IGameLogger logger)
        {
            _entryAnimator = entryAnimator;
            _enemyDataProvider = enemyDataProvider;
            _hexDirectionConfig = hexDirectionConfig;
            _runSeedProvider = runSeedProvider;
            _unitViewRegistry = unitViewRegistry;
            _container = container;
            _logger = logger;
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

            IAIDecisionMaker decisionMaker = CreateDecisionMaker(enemyId, enemyData.AIType, aiProfile);

            var playerId = _nextPlayerId++;
            var player = new AIPlayer(
                id: playerId,
                name: enemyData.Name,
                decisionMaker: decisionMaker);

            string aiDescription = aiProfile != null
                ? $"{enemyData.AIType} (Configurable)"
                : enemyData.AIType.ToString();
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Created AIPlayer: ID={playerId}, Name={enemyData.Name}, AI={aiDescription}");

            return player;
        }

        /// <summary>
        /// Creates the appropriate AI decision maker based on personality and profile.
        /// Each decision maker is seeded from the run seed so enemy plans are reproducible
        /// (same seed → same committed intents, the intent-phase determinism guarantee).
        /// </summary>
        private IAIDecisionMaker CreateDecisionMaker(int enemyId, AIPersonality personality, AIProfileDefinition profile)
        {
            int seed = Loot.Core.LootSeed.Derive(_runSeedProvider.RunSeed, $"{AiSeedContext}:{enemyId}");

            // If we have an AI profile, use ConfigurableTacticalAI for Tactical personality
            if (profile != null && personality == AIPersonality.Tactical)
            {
                return new ConfigurableTacticalAI(profile, seed, _logger);
            }

            // Fallback to standard AI implementations
            return personality switch
            {
                AIPersonality.SimpleRandom => new SimpleRandomAI(seed, _logger),
                AIPersonality.Tactical => new TacticalAI(seed, _logger),
                _ => new SimpleRandomAI(seed, _logger)
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
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Starting integration for enemy {enemyId}");

            // Get enemy data
            EnemyData enemyData = _enemyDataProvider.GetEnemyData(enemyId);

            // Find closest battlefield cell to platform center
            HexCoordinates startCell = _entryAnimator.FindClosestCell(platformCenter, battlefield);
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Found closest cell for enemy: {startCell}");

            // Reposition existing enemy GameObject to battlefield cell, feet on the surface top
            // (runs before InitializeForCombat, so the offset is derived locally here).
            Vector3 worldPosition = UnitGrounding.Grounded(
                battlefield.HexToWorld(startCell),
                UnitGrounding.FeetOffsetFor(existingComponent.transform));
            existingComponent.transform.position = worldPosition;
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Repositioned enemy {enemyId} to {worldPosition}");

            // Make Rigidbody kinematic during combat to prevent physics interference
            var rb = existingComponent.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Set enemy {enemyId} Rigidbody to kinematic for combat");
            }

            // Initialize component for combat
            int unitId = _nextUnitId++;
            existingComponent.InitializeForCombat(unitId, enemyPlayer, startCell, combatController, enemyData);
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Initialized EnemyCombatComponent for combat");

            // Add internal Unit to combat state (NOT the MonoBehaviour component)
            combatController.AddUnit(existingComponent.InternalUnit);
            _logger.Info(LogCategory.Combat,$"[EnemyCombatIntegrator] Enemy {enemyId} integrated: UnitID={unitId}, Cell={startCell}, Position={worldPosition}");

            // Keep the enemy model's yaw in sync with its domain facing — reading an enemy's
            // facing is reading where its committed blow will land.
            var facingRotator = existingComponent.GetComponent<View.UnitFacingRotator>();
            if (facingRotator == null)
                facingRotator = existingComponent.gameObject.AddComponent<View.UnitFacingRotator>();
            facingRotator.Initialize(combatController, battlefield, _hexDirectionConfig, unitId);

            // Presentation (overhead plan icons, ghost clones) finds this unit's visual here.
            _unitViewRegistry.Register(unitId, existingComponent.transform);

            yield return null;
        }
    }
}
