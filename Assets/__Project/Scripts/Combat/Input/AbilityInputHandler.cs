using System;
using Combat.Battlefield;
using Combat.Config;
using Combat.Input.Commands;
using Combat.Player;
using UnityEngine;

namespace Combat.Input
{
    /// <summary>
    /// Handles ability input commands and delegates to CombatAbilityPresenter.
    /// Pure C# class — no Unity dependencies except Vector3.
    /// </summary>
    public class AbilityInputHandler : IDisposable
    {
        private readonly IInputController _inputController;
        private readonly HexDirectionConfig _hexConfig;

        private CombatAbilityPresenter _presenter;
        private Func<HexCoordinates> _getCurrentPosition;
        private Func<bool> _isPlayerTurnCheck;

        private bool _isAbilityModeActive;
        private bool _isDisposed;

        public AbilityInputHandler(
            IInputController inputController,
            HexDirectionConfig hexConfig)
        {
            _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
            _hexConfig = hexConfig ?? throw new ArgumentNullException(nameof(hexConfig));

            SubscribeToEvents();
        }

        public void Bind(
            CombatAbilityPresenter presenter,
            Func<HexCoordinates> getCurrentPosition,
            Func<bool> isPlayerTurnCheck)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _getCurrentPosition = getCurrentPosition ?? throw new ArgumentNullException(nameof(getCurrentPosition));
            _isPlayerTurnCheck = isPlayerTurnCheck ?? throw new ArgumentNullException(nameof(isPlayerTurnCheck));
        }

        public void Unbind()
        {
            _presenter = null;
            _getCurrentPosition = null;
            _isPlayerTurnCheck = null;
            _isAbilityModeActive = false;
        }

        private void SubscribeToEvents()
        {
            _inputController.OnAbilitySelected += HandleAbilitySelected;
            _inputController.OnAbilityCancelled += HandleAbilityCancelled;
            _inputController.OnAbilityConfirmed += HandleAbilityConfirmed;
            _inputController.OnMovementDirectionChanged += HandleMouseMoved;
            _inputController.OnMovementCancelled += HandleMovementCancelled;
            _inputController.OnExecuteQueueRequested += HandleExecuteQueue;
        }

        private void UnsubscribeFromEvents()
        {
            _inputController.OnAbilitySelected -= HandleAbilitySelected;
            _inputController.OnAbilityCancelled -= HandleAbilityCancelled;
            _inputController.OnAbilityConfirmed -= HandleAbilityConfirmed;
            _inputController.OnMovementDirectionChanged -= HandleMouseMoved;
            _inputController.OnMovementCancelled -= HandleMovementCancelled;
            _inputController.OnExecuteQueueRequested -= HandleExecuteQueue;
        }

        private void HandleAbilitySelected(AbilitySelectedCommand command)
        {
            if (!CanProcessInput()) return;

            Debug.Log($"[AbilityInputHandler] Ability {command.AbilityIndex} selected");
            _isAbilityModeActive = true;
            _presenter?.SelectAbility(command.AbilityIndex);
        }

        private void HandleMouseMoved(MovementDirectionChangedCommand command)
        {
            if (!CanProcessInput()) return;
            if (!_isAbilityModeActive) return;

            HexDirection? direction = command.WorldDirection.HasValue
                ? DirectionToHexConverter.GetHexDirection(command.WorldDirection.Value, _hexConfig)
                : (HexDirection?)null;

            _presenter?.UpdateAimDirection(direction);
        }

        private void HandleAbilityConfirmed(AbilityConfirmedCommand command)
        {
            if (!CanProcessInput()) return;
            if (!_isAbilityModeActive) return;

            Debug.Log("[AbilityInputHandler] Ability confirmed");
            _presenter?.ConfirmAim();
            _isAbilityModeActive = false;
        }

        private void HandleAbilityCancelled(AbilityCancelledCommand command)
        {
            if (!_isAbilityModeActive) return;

            Debug.Log("[AbilityInputHandler] Ability targeting cancelled");
            _presenter?.CancelAbilitySelection();
            _isAbilityModeActive = false;
        }

        private void HandleMovementCancelled(MovementCancelledCommand command)
        {
            // Right-click also cancels ability targeting if active
            if (_isAbilityModeActive)
            {
                _presenter?.CancelAbilitySelection();
                _isAbilityModeActive = false;
            }
        }

        private void HandleExecuteQueue(ExecuteQueueCommand command)
        {
            if (!CanProcessInput()) return;

            Debug.Log("[AbilityInputHandler] Execute queue requested");
            _presenter?.ExecuteQueue();
        }

        private bool CanProcessInput()
        {
            if (_presenter == null) return false;
            if (_isPlayerTurnCheck == null) return false;
            if (!_isPlayerTurnCheck()) return false;
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            UnsubscribeFromEvents();
            Unbind();
            _isDisposed = true;
        }
    }
}
