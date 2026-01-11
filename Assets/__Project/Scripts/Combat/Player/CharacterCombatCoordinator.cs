using Character;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Input;
using Combat.View;
using UnityEngine;
using Zenject;

namespace Combat.Player
{
    /// <summary>
    /// Thin MonoBehaviour coordinator that wires together all combat movement components.
    /// Follows MVP pattern - delegates to presenters and handlers.
    /// </summary>
    public class CharacterCombatCoordinator : MonoBehaviour
    {
        private CombatMovementInputHandler _inputHandler;
        private CombatMovementPresenter _presenter;
        private CharacterCombatAnimator _animator;
        private ICellHighlightService _highlightService;

        // Global dependencies injected by Zenject
        [Inject] private IInputController _inputController;
        [Inject] private ICharacterMovementAnimator _animationStrategy;
        [Inject] private HexDirectionConfig _hexConfig;
        [Inject] private CombatMovementConfig _movementConfig;

        // Platform-scoped dependencies passed manually
        private ICombatController _combatController;
        private IBattlefield _battlefield;

        private CharacterCombatComponent _characterUnit;
        
        public void Initialize(
            CharacterCombatComponent characterUnit,
            ICombatController combatController,
            IBattlefield battlefield)
        {
            _characterUnit = characterUnit;
            _combatController = combatController;
            _battlefield = battlefield;

            Debug.Log($"[CharacterCombatCoordinator] Initializing for unit {_characterUnit.Id}");

            // Create highlight service with battlefield
            _highlightService = new CellHighlightService(_movementConfig, _battlefield);

            // Create handler and presenter
            _inputHandler = new CombatMovementInputHandler(_inputController, _hexConfig);
            _presenter = new CombatMovementPresenter(
                _combatController,
                _battlefield,
                _highlightService,
                _characterUnit);

            // Setup animator
            _animator = gameObject.AddComponent<CharacterCombatAnimator>();
            _animator.Initialize(
                _animationStrategy,
                _battlefield,
                _combatController,
                _characterUnit.Id);

            Debug.Log("[CharacterCombatCoordinator] Initialization complete");
        }
        
        private void Update()
        {
            // Early exit if not initialized yet
            if (_characterUnit == null)
            {
                return; // Silent return during initialization
            }
            
            if (_inputHandler == null || _presenter == null)
            {
                return; // Silent return during initialization
            }
            
            bool isPlayerTurn = IsPlayerTurn();
            if (!isPlayerTurn)
            {
                return; // Silent return - not our turn
            }
            
            bool isActive = _inputHandler.IsActive;
            
            if (isActive)
            {
                var targetCell = _inputHandler.GetTargetCell(_characterUnit.Position);
                
                if (targetCell.HasValue)
                {
                    Debug.Log($"[CharacterCombatCoordinator] Movement active, target cell: {targetCell.Value}");
                }
                
                _presenter.UpdateHighlight(targetCell);
                
                if (_inputHandler.IsConfirmed && targetCell.HasValue)
                {
                    Debug.Log($"[CharacterCombatCoordinator] Confirm pressed! Attempting move to cell {targetCell.Value}");
                    _presenter.TryMoveToCell(targetCell.Value);
                }
            }
            else
            {
                _presenter.UpdateHighlight(null); // Clear highlight
            }
        }
        
        private bool IsPlayerTurn()
        {
            if (_combatController?.TurnManager == null)
            {
                return false;
            }
            
            if (_combatController.TurnManager.CurrentPlayer == null)
            {
                return false;
            }
            
            if (_characterUnit?.Owner == null)
            {
                return false;
            }
            
            bool isMatch = _combatController.TurnManager.CurrentPlayer.Id == _characterUnit.Owner.Id;
            return isMatch;
        }
        
        private void OnDestroy()
        {
            if (_animator != null)
            {
                Destroy(_animator);
            }
        }
    }
}
