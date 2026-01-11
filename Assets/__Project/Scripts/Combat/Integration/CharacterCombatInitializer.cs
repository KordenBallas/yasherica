using System.Collections;
using System.Collections.Generic;
using Character;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Input;
using Combat.Player;
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
        private readonly CombatMovementConfig _config;
        private readonly DiContainer _container;
        private readonly IInputController _inputController;
        
        public CharacterCombatInitializer(
            ICharacterRegistry characterRegistry,
            CombatEntryAnimator entryAnimator,
            CombatMovementConfig config,
            DiContainer container,
            IInputController inputController)
        {
            _characterRegistry = characterRegistry;
            _entryAnimator = entryAnimator;
            _config = config;
            _container = container;
            _inputController = inputController;
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
                Debug.LogError("[CharacterCombatInitializer] No player character found in registry");
                yield break;
            }
            
            Debug.Log($"[CharacterCombatInitializer] Initializing character {character.name} for combat");
            
            // Find closest battlefield cell
            HexCoordinates startCell = _entryAnimator.FindClosestCell(character.position, battlefield);
            
            // Animate character to cell
            yield return _entryAnimator.AnimateEntryToCell(character, startCell, battlefield);
            
            // Get or create CharacterCombatComponent
            var combatComponent = character.GetComponent<CharacterCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = character.gameObject.AddComponent<CharacterCombatComponent>();
                Debug.Log("[CharacterCombatInitializer] Added CharacterCombatComponent");
            }
            
            // Initialize component
            int unitId = GenerateUnitId();
            combatComponent.InitializeForCombat(unitId, player, startCell, combatController);

            Debug.Log($"[CharacterCombatInitializer] Character initialized: ID={unitId}, Cell={startCell}");

            // Add internal Unit to combat state (NOT the MonoBehaviour component)
            combatController.AddUnit(combatComponent.InternalUnit);
            Debug.Log($"[CharacterCombatInitializer] Added internal Unit (not component) to combat state");
            
            // Disable CharacterMovementController
            var movementController = character.GetComponent<CharacterMovementController>();
            if (movementController != null)
            {
                movementController.enabled = false;
                Debug.Log("[CharacterCombatInitializer] Disabled CharacterMovementController");
            }
            
            // Attach and initialize CharacterCombatCoordinator
            var coordinator = character.GetComponent<CharacterCombatCoordinator>();
            if (coordinator == null)
            {
                coordinator = character.gameObject.AddComponent<CharacterCombatCoordinator>();
                Debug.Log("[CharacterCombatInitializer] Added CharacterCombatCoordinator component");
            }
            
            // Inject global dependencies (those with [Inject] attributes)
            _container.Inject(coordinator);
            
            // Initialize coordinator with combat component and platform-scoped dependencies
            coordinator.Initialize(combatComponent, combatController, battlefield);
            Debug.Log("[CharacterCombatInitializer] CharacterCombatCoordinator initialized and ready");
            
            // Set character transform on input controller for direction calculations
            if (_inputController is Input.PCInputController pcInput)
            {
                pcInput.SetCharacterTransform(character);
                Debug.Log("[CharacterCombatInitializer] Character transform set on PCInputController");
            }
            else
            {
                Debug.LogWarning($"[CharacterCombatInitializer] Input controller is not PCInputController, type: {_inputController?.GetType().Name ?? "null"}");
            }
            
            // Verify combat setup
            Debug.Log($"[CharacterCombatInitializer] Combat setup complete for {character.name}");
            Debug.Log($"[CharacterCombatInitializer] Unit ID: {combatComponent.Id}, Owner: {combatComponent.Owner?.Id ?? -1}, Position: {combatComponent.Position}");
            
            if (combatController?.TurnManager != null)
            {
                var currentPlayer = combatController.TurnManager.CurrentPlayer;
                Debug.Log($"[CharacterCombatInitializer] Turn System Status:");
                Debug.Log($"  - Current Player ID: {currentPlayer?.Id ?? -1}");
                Debug.Log($"  - Character Owner ID: {combatComponent.Owner?.Id ?? -1}");
                Debug.Log($"  - Is Player's Turn: {currentPlayer?.Id == combatComponent.Owner?.Id}");
                Debug.Log($"  - Turn Number: {combatController.TurnManager.CurrentTurnNumber}");
            }
            else
            {
                Debug.LogError("[CharacterCombatInitializer] TurnManager is NULL! Combat input will not work!");
            }
        }
        
        private int GenerateUnitId()
        {
            // Simple ID generation - in production, use proper ID service
            return UnityEngine.Random.Range(1000, 9999);
        }
    }
}
