using System;
using Combat.Battlefield;
using Combat.Config;
using Combat.Input.Commands;
using Combat.Player;
using Core.Logging;
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
        private readonly IGameLogger _logger;

        private CombatAbilityPresenter _presenter;
        private Func<HexCoordinates> _getCurrentPosition;
        private Func<bool> _isPlayerTurnCheck;

        private bool _isAbilityModeActive;
        private bool _isVolleyAimActive;
        private bool _isDisposed;

        public AbilityInputHandler(
            IInputController inputController,
            HexDirectionConfig hexConfig,
            IGameLogger logger)
        {
            _logger = logger;
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
            _isVolleyAimActive = false;
        }

        private void SubscribeToEvents()
        {
            _inputController.OnAbilitySelected += HandleAbilitySelected;
            _inputController.OnAbilityCancelled += HandleAbilityCancelled;
            _inputController.OnAbilityConfirmed += HandleAbilityConfirmed;
            _inputController.OnMovementDirectionChanged += HandleMouseMoved;
            _inputController.OnMovementCancelled += HandleMovementCancelled;
            _inputController.OnExecuteQueueRequested += HandleExecuteQueue;
            _inputController.OnVolleyAimStarted += HandleVolleyAimStarted;
            _inputController.OnVolleyAimCancelled += HandleVolleyAimCancelled;
        }

        private void UnsubscribeFromEvents()
        {
            _inputController.OnAbilitySelected -= HandleAbilitySelected;
            _inputController.OnAbilityCancelled -= HandleAbilityCancelled;
            _inputController.OnAbilityConfirmed -= HandleAbilityConfirmed;
            _inputController.OnMovementDirectionChanged -= HandleMouseMoved;
            _inputController.OnMovementCancelled -= HandleMovementCancelled;
            _inputController.OnExecuteQueueRequested -= HandleExecuteQueue;
            _inputController.OnVolleyAimStarted -= HandleVolleyAimStarted;
            _inputController.OnVolleyAimCancelled -= HandleVolleyAimCancelled;
        }

        private void HandleAbilitySelected(AbilitySelectedCommand command)
        {
            if (!CanProcessInput()) return;

            _logger.Info(LogCategory.Combat,$"[AbilityInputHandler] Ability {command.AbilityIndex} selected");
            _isAbilityModeActive = true;
            _presenter?.SelectAbility(command.AbilityIndex);
        }

        private void HandleMouseMoved(MovementDirectionChangedCommand command)
        {
            if (!CanProcessInput()) return;
            if (!_isAbilityModeActive && !_isVolleyAimActive) return;

            HexDirection? direction = command.WorldDirection.HasValue
                ? DirectionToHexConverter.GetHexDirection(command.WorldDirection.Value, _hexConfig)
                : (HexDirection?)null;

            if (_isVolleyAimActive)
                _presenter?.UpdateVolleyAim(direction); // turn the whole queued volley toward the cursor (D2)
            else
                _presenter?.UpdateAimDirection(direction);
        }

        private void HandleVolleyAimStarted(VolleyAimStartedCommand command)
        {
            if (!CanProcessInput()) return;
            // An ability aim in progress takes precedence — don't start a volley aim on top of it.
            if (_isAbilityModeActive) return;

            _logger.Info(LogCategory.Combat,"[AbilityInputHandler] Volley aim started");
            _isVolleyAimActive = true;
            _presenter?.BeginVolleyAim();
        }

        private void HandleVolleyAimCancelled(VolleyAimCancelledCommand command)
        {
            if (!_isVolleyAimActive) return;

            _logger.Info(LogCategory.Combat,"[AbilityInputHandler] Volley aim cancelled");
            _presenter?.EndVolleyAim();
            _isVolleyAimActive = false;
        }

        private void HandleAbilityConfirmed(AbilityConfirmedCommand command)
        {
            if (!_isAbilityModeActive) return;

            // The turn may have ended while the key was held; the aim highlight must still clear
            // (D8: highlights always reset when the plan phase closes).
            if (!CanProcessInput())
            {
                _presenter?.CancelAbilitySelection();
                _isAbilityModeActive = false;
                return;
            }

            _logger.Info(LogCategory.Combat,"[AbilityInputHandler] Ability confirmed");
            _presenter?.ConfirmAim();
            _isAbilityModeActive = false;
        }

        private void HandleAbilityCancelled(AbilityCancelledCommand command)
        {
            if (!_isAbilityModeActive) return;

            _logger.Info(LogCategory.Combat,"[AbilityInputHandler] Ability targeting cancelled");
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
            // The volley highlight clears unconditionally — even when the turn check no longer
            // passes, the plan-phase highlight must not outlive the gesture (D8).
            if (_isVolleyAimActive)
            {
                _presenter?.EndVolleyAim();
                _isVolleyAimActive = false;
            }

            if (!CanProcessInput()) return;

            _logger.Info(LogCategory.Combat,"[AbilityInputHandler] Execute queue requested");
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
