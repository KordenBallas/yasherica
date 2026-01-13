using Character;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Input;
using UnityEngine;
using Zenject;

namespace Combat.Player
{
    /// <summary>
    /// Thin MonoBehaviour coordinator that wires together all combat movement components.
    /// Follows MVP pattern - delegates to presenters and handlers.
    /// Uses event-driven InputCommandHandler instead of polling.
    /// </summary>
    public class CharacterCombatCoordinator : MonoBehaviour
    {
        // New: Pure C# command handler (replaces CombatMovementInputHandler)
        private InputCommandHandler _commandHandler;
        private CombatMovementPresenter _presenter;
        private CharacterCombatAnimator _animator;
        private HexCellController _cellController;

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

            // Create cell controller (manages cell state transitions)
            _cellController = new HexCellController(_battlefield, _movementConfig);

            // Create presenter
            _presenter = new CombatMovementPresenter(
                _combatController,
                _battlefield,
                _cellController,
                _characterUnit);

            // Create and bind command handler (event-driven input handling)
            _commandHandler = new InputCommandHandler(_inputController, _hexConfig);
            _commandHandler.Bind(
                _presenter,
                () => _characterUnit.Position,
                IsPlayerTurn);

            // Setup animator
            _animator = gameObject.AddComponent<CharacterCombatAnimator>();
            _animator.Initialize(
                _animationStrategy,
                _battlefield,
                _combatController,
                _characterUnit.Id);

            Debug.Log("[CharacterCombatCoordinator] Initialization complete");
        }

        // Note: Update() method removed entirely.
        // All input handling is now event-driven via InputCommandHandler.

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
            _commandHandler?.Dispose();

            if (_animator != null)
            {
                Destroy(_animator);
            }
        }
    }
}
