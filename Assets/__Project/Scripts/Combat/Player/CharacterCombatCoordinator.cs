using Character;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Input;
using Combat.Integration;
using Combat.View;
using UnityEngine;
using Zenject;

namespace Combat.Player
{
    /// <summary>
    /// Thin MonoBehaviour coordinator that wires together all combat components.
    /// Follows MVP pattern - delegates to presenters and handlers.
    /// </summary>
    public class CharacterCombatCoordinator : MonoBehaviour
    {
        // Input handlers
        private InputCommandHandler _movementHandler;
        private AbilityInputHandler _abilityHandler;
        private CombatInputModeManager _inputModeManager;

        // Presenters
        private CombatMovementPresenter _movementPresenter;
        private CombatAbilityPresenter _abilityPresenter;
        private CombatActionPanelPresenter _actionPanelPresenter;

        // Other components
        private CharacterCombatAnimator _animator;
        private HexCellController _cellController;

        // Injected dependencies
        [Inject] private IInputController _inputController;
        [Inject] private ICharacterMovementAnimator _animationStrategy;
        [Inject] private HexDirectionConfig _hexConfig;
        [Inject] private CombatMovementConfig _movementConfig;
        [Inject] private IAbilityShapeCalculator _shapeCalculator;
        [Inject] private InputConfig _inputConfig;
        [Inject(Optional = true)] private ICombatActionPanelView _actionPanelView;
        [Inject] private CharacterCombatInitializer _characterInitializer;

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

            _cellController = new HexCellController(_battlefield, _movementConfig);

            _movementPresenter = new CombatMovementPresenter(
                _combatController,
                _battlefield,
                _cellController,
                _characterUnit);

            _abilityPresenter = new CombatAbilityPresenter(
                _combatController,
                _battlefield,
                _cellController,
                _shapeCalculator,
                _characterUnit);

            _inputModeManager = new CombatInputModeManager();

            _movementHandler = new InputCommandHandler(_inputController, _hexConfig);
            _movementHandler.Bind(
                _movementPresenter,
                () => _characterUnit.Position,
                IsPlayerTurn);

            _abilityHandler = new AbilityInputHandler(_inputController, _hexConfig);
            _abilityHandler.Bind(
                _abilityPresenter,
                () => _characterUnit.Position,
                IsPlayerTurn);

            if (_actionPanelView != null)
            {
                _actionPanelPresenter = new CombatActionPanelPresenter(
                    _actionPanelView,
                    _combatController,
                    _inputConfig);

                var abilityDefinitions = _characterInitializer.CharacterAbilityDefinitions;
                _actionPanelPresenter.SetUnit(_characterUnit, abilityDefinitions);
            }
            else
            {
                Debug.LogWarning("[CharacterCombatCoordinator] No ICombatActionPanelView found - UI will not be available");
            }

            _animator = gameObject.AddComponent<CharacterCombatAnimator>();
            _animator.Initialize(
                _animationStrategy,
                _battlefield,
                _combatController,
                _characterUnit.Id);

            Debug.Log("[CharacterCombatCoordinator] Initialization complete");
        }

        private bool IsPlayerTurn()
        {
            if (_combatController?.TurnManager == null) return false;
            if (_combatController.TurnManager.CurrentPlayer == null) return false;
            if (_characterUnit?.Owner == null) return false;

            return _combatController.TurnManager.CurrentPlayer.Id == _characterUnit.Owner.Id;
        }

        private void OnDestroy()
        {
            _movementHandler?.Dispose();
            _abilityHandler?.Dispose();
            _inputModeManager?.Dispose();
            _abilityPresenter?.Dispose();
            _actionPanelPresenter?.Dispose();

            if (_animator != null)
                Destroy(_animator);
        }
    }
}
