using System.Collections;
using System.Collections.Generic;
using Character;
using CharacterSystem.Runtime;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Input;
using Combat.Player;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Integration
{
    /// <summary>
    /// Service for initializing character for combat.
    /// Single responsibility: character combat setup and integration.
    /// </summary>
    public class CharacterCombatInitializer
    {
        private readonly ICharacterRegistry _characterRegistry;
        private readonly CombatEntryAnimator _entryAnimator;
        private readonly ICharacterMovementAnimator _movementAnimator;
        private readonly CombatMovementConfig _config;
        private readonly HexDirectionConfig _hexDirectionConfig;
        private readonly View.ICombatUnitViewRegistry _unitViewRegistry;
        private readonly DiContainer _container;
        private readonly IInputController _inputController;
        private readonly IAbilityFactory _abilityFactory;
        private readonly IStatusEffectFactory _statusEffectFactory;
        private readonly IPartAbilityResolver _partAbilityResolver;
        private readonly HeroDefinition _heroDefinition;
        private readonly IGameLogger _logger;
        private List<AbilityDefinition> _characterAbilityDefinitions;

        // Passives are standing modifiers that last the whole combat: applied with an
        // infinite (negative) duration so the turn loop never decrements them away.
        private const int PermanentEffectDuration = -1;

        public CharacterCombatInitializer(
            ICharacterRegistry characterRegistry,
            CombatEntryAnimator entryAnimator,
            ICharacterMovementAnimator movementAnimator,
            CombatMovementConfig config,
            HexDirectionConfig hexDirectionConfig,
            View.ICombatUnitViewRegistry unitViewRegistry,
            DiContainer container,
            IInputController inputController,
            IAbilityFactory abilityFactory,
            IStatusEffectFactory statusEffectFactory,
            IPartAbilityResolver partAbilityResolver,
            HeroDefinition heroDefinition,
            IGameLogger logger)
        {
            _characterRegistry = characterRegistry;
            _entryAnimator = entryAnimator;
            _movementAnimator = movementAnimator;
            _config = config;
            _hexDirectionConfig = hexDirectionConfig;
            _unitViewRegistry = unitViewRegistry;
            _container = container;
            _inputController = inputController;
            _abilityFactory = abilityFactory;
            _statusEffectFactory = statusEffectFactory;
            _partAbilityResolver = partAbilityResolver;
            _heroDefinition = heroDefinition;
            _logger = logger;
        }
        
        /// <summary>
        /// Initializes the character for combat.
        /// Finds character, animates to closest cell, creates combat component, and adds to state.
        /// </summary>
        public IEnumerator InitializeCharacterForCombat(
            IPlayer player,
            IBattlefield battlefield,
            ICombatController combatController)
        {
            // Get character from registry
            Transform character = _characterRegistry.GetPlayerCharacter();
            if (character == null)
            {
                _logger.Error(LogCategory.Combat,"[CharacterCombatInitializer] No player character found in registry");
                yield break;
            }
            
            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Initializing character {character.name} for combat");
            
            // One placement rule for every unit (D6): the closest FREE cell to the unit's own
            // position — the hero can never land on a cell an already-integrated unit holds.
            HexCoordinates startCell = SpawnCellResolver.Resolve(
                character.position, battlefield, combatController.CombatState);

            // Animate character to cell
            yield return _entryAnimator.AnimateEntryToCell(character, startCell, battlefield);
            
            // Get or create CharacterCombatComponent
            var combatComponent = character.GetComponent<CharacterCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = character.gameObject.AddComponent<CharacterCombatComponent>();
                _logger.Info(LogCategory.Combat,"[CharacterCombatInitializer] Added CharacterCombatComponent");
            }
            
            // Build the combat ability set from the player's equipped body parts: parts are the
            // source of truth for what the player can do in combat, so mutating the body changes
            // the ability set on the next combat. HeroDefinition abilities are a temporary
            // workaround used only as a fallback when no equipped part grants an active ability.
            var abilityInstances = new List<IAbilityInstance>();
            var passiveEffects = new List<IStatusEffect>();
            _characterAbilityDefinitions = new List<AbilityDefinition>();

            var modular = character.GetComponentInChildren<ModularCharacterVisual>()?.Character;
            var partAbilities = modular != null
                ? _partAbilityResolver.Resolve(modular.EquippedParts.Values)
                : PartAbilitySet.Empty;

            foreach (var abilityDef in partAbilities.ActiveAbilities)
            {
                abilityInstances.Add(_abilityFactory.CreateAbilityInstance(abilityDef));
                _characterAbilityDefinitions.Add(abilityDef);
            }

            if (abilityInstances.Count == 0)
            {
                _logger.Warning(LogCategory.Combat,"[CharacterCombatInitializer] No part-granted active abilities found; falling back to HeroDefinition abilities.");
                if (_heroDefinition != null && _heroDefinition.Abilities != null)
                {
                    foreach (var abilityDef in _heroDefinition.Abilities)
                    {
                        abilityInstances.Add(_abilityFactory.CreateAbilityInstance(abilityDef));
                        _characterAbilityDefinitions.Add(abilityDef);
                    }
                }
            }

            // Part-granted passives become standing modifiers applied for the whole combat.
            foreach (var passive in partAbilities.PassiveAbilities)
            {
                if (passive == null || passive.Modifier == null)
                {
                    continue;
                }

                passiveEffects.Add(_statusEffectFactory.CreateStatusEffect(passive.Modifier, PermanentEffectDuration));
            }

            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Combat ability set: {abilityInstances.Count} active, {passiveEffects.Count} passive");

            // Initialize component with abilities and standing passive modifiers
            int unitId = GenerateUnitId();
            int maxHP = _heroDefinition?.MaxHP ?? 100;
            combatComponent.InitializeForCombat(unitId, player, startCell, combatController, maxHP, abilityInstances, passiveEffects,
                _config, _movementAnimator, _hexDirectionConfig);

            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Character initialized: ID={unitId}, Cell={startCell}, MaxHP={maxHP}");

            // Add internal Unit to combat state (NOT the MonoBehaviour component)
            combatController.AddUnit(combatComponent.InternalUnit);
            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Added internal Unit (not component) to combat state");

            // Keep the model's yaw in sync with the domain facing (facing legibility).
            var facingRotator = character.GetComponent<View.UnitFacingRotator>();
            if (facingRotator == null)
                facingRotator = character.gameObject.AddComponent<View.UnitFacingRotator>();
            facingRotator.Initialize(combatController, battlefield, _hexDirectionConfig, unitId);

            // Presentation (overhead plan icons, ghost clones) finds this unit's visual here.
            _unitViewRegistry.Register(unitId, character);


            // Disable CharacterMovementController
            var movementController = character.GetComponent<CharacterMovementController>();
            if (movementController != null)
            {
                movementController.enabled = false;
                _logger.Info(LogCategory.Combat,"[CharacterCombatInitializer] Disabled CharacterMovementController");
            }
            
            // Attach and initialize CharacterCombatCoordinator
            var coordinator = character.GetComponent<CharacterCombatCoordinator>();
            if (coordinator == null)
            {
                coordinator = character.gameObject.AddComponent<CharacterCombatCoordinator>();
                _logger.Info(LogCategory.Combat,"[CharacterCombatInitializer] Added CharacterCombatCoordinator component");
            }
            
            // Inject global dependencies (those with [Inject] attributes)
            _container.Inject(coordinator);
            
            // Initialize coordinator with combat component and platform-scoped dependencies
            coordinator.Initialize(combatComponent, combatController, battlefield);
            _logger.Info(LogCategory.Combat,"[CharacterCombatInitializer] CharacterCombatCoordinator initialized and ready");
            
            // Set character transform on input controller for direction calculations
            if (_inputController is Input.CombatInputController combatInput)
            {
                combatInput.SetCharacterTransform(character);
                _logger.Info(LogCategory.Combat,"[CharacterCombatInitializer] Character transform set on CombatInputController");
            }
            else
            {
                _logger.Warning(LogCategory.Combat,$"[CharacterCombatInitializer] Input controller is not CombatInputController, type: {_inputController?.GetType().Name ?? "null"}");
            }
            
            // Verify combat setup
            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Combat setup complete for {character.name}");
            _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Unit ID: {combatComponent.Id}, Owner: {combatComponent.Owner?.Id ?? -1}, Position: {combatComponent.Position}");
            
            if (combatController?.TurnManager != null)
            {
                var currentPlayer = combatController.TurnManager.CurrentPlayer;
                _logger.Info(LogCategory.Combat,$"[CharacterCombatInitializer] Turn System Status:");
                _logger.Info(LogCategory.Combat,$"  - Current Player ID: {currentPlayer?.Id ?? -1}");
                _logger.Info(LogCategory.Combat,$"  - Character Owner ID: {combatComponent.Owner?.Id ?? -1}");
                _logger.Info(LogCategory.Combat,$"  - Is Player's Turn: {currentPlayer?.Id == combatComponent.Owner?.Id}");
                _logger.Info(LogCategory.Combat,$"  - Turn Number: {combatController.TurnManager.CurrentTurnNumber}");
            }
            else
            {
                _logger.Error(LogCategory.Combat,"[CharacterCombatInitializer] TurnManager is NULL! Combat input will not work!");
            }
        }
        
        private int GenerateUnitId()
        {
            // Simple ID generation - in production, use proper ID service
            return UnityEngine.Random.Range(1000, 9999);
        }

        public IReadOnlyList<AbilityDefinition> CharacterAbilityDefinitions => _characterAbilityDefinitions;
    }
}
