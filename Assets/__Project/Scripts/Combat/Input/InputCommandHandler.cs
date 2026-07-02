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
    /// Handles input commands and delegates to CombatMovementPresenter.
    /// Pure C# class - no Unity dependencies except Vector3.
    /// Single responsibility: translating input commands to presenter calls.
    /// </summary>
    public class InputCommandHandler : IDisposable
    {
        private readonly IInputController _inputController;
        private readonly HexDirectionConfig _hexConfig;
        private readonly IGameLogger _logger;

        // Presenter is set via method injection (not constructor)
        // because it depends on character-specific state
        private CombatMovementPresenter _presenter;
        private Func<HexCoordinates> _getCurrentPosition;
        private Func<bool> _isPlayerTurnCheck;

        // State
        private bool _isMovementModeActive;
        private HexCoordinates? _currentTargetCell;
        private bool _isDisposed;

        /// <summary>
        /// Event fired when a command is processed (for debugging/logging).
        /// </summary>
        public event Action<IInputCommand> OnCommandProcessed;

        public InputCommandHandler(
            IInputController inputController,
            HexDirectionConfig hexConfig,
            IGameLogger logger)
        {
            _logger = logger;
            _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
            _hexConfig = hexConfig ?? throw new ArgumentNullException(nameof(hexConfig));

            SubscribeToEvents();
        }

        /// <summary>
        /// Binds the handler to a specific presenter and character.
        /// Must be called before input can be processed.
        /// </summary>
        /// <param name="presenter">The presenter to delegate to</param>
        /// <param name="getCurrentPosition">Function to get current unit position</param>
        /// <param name="isPlayerTurnCheck">Function to check if it's player's turn</param>
        public void Bind(
            CombatMovementPresenter presenter,
            Func<HexCoordinates> getCurrentPosition,
            Func<bool> isPlayerTurnCheck)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _getCurrentPosition = getCurrentPosition ?? throw new ArgumentNullException(nameof(getCurrentPosition));
            _isPlayerTurnCheck = isPlayerTurnCheck ?? throw new ArgumentNullException(nameof(isPlayerTurnCheck));
        }

        /// <summary>
        /// Unbinds the handler from its presenter.
        /// </summary>
        public void Unbind()
        {
            _presenter = null;
            _getCurrentPosition = null;
            _isPlayerTurnCheck = null;

            // Clear state
            _isMovementModeActive = false;
            _currentTargetCell = null;
        }

        private void SubscribeToEvents()
        {
            _inputController.OnMovementModeChanged += HandleMovementModeChanged;
            _inputController.OnMovementDirectionChanged += HandleMovementDirectionChanged;
            _inputController.OnMovementConfirmed += HandleMovementConfirmed;
            _inputController.OnMovementCancelled += HandleMovementCancelled;
        }

        private void UnsubscribeFromEvents()
        {
            _inputController.OnMovementModeChanged -= HandleMovementModeChanged;
            _inputController.OnMovementDirectionChanged -= HandleMovementDirectionChanged;
            _inputController.OnMovementConfirmed -= HandleMovementConfirmed;
            _inputController.OnMovementCancelled -= HandleMovementCancelled;
        }

        private void HandleMovementModeChanged(MovementModeChangedCommand command)
        {
            if (!CanProcessInput()) return;

            _isMovementModeActive = command.IsActive;

            if (!_isMovementModeActive)
            {
                // Clear highlight when movement mode deactivates
                _currentTargetCell = null;
                _presenter?.UpdateHighlight(null);
            }

            OnCommandProcessed?.Invoke(command);
            _logger.Info(LogCategory.Combat,$"[InputCommandHandler] Movement mode changed: {command.IsActive}");
        }

        private void HandleMovementDirectionChanged(MovementDirectionChangedCommand command)
        {
            if (!CanProcessInput()) return;
            if (!_isMovementModeActive) return;

            // Convert world direction to hex coordinates
            _currentTargetCell = command.WorldDirection.HasValue
                ? DirectionToHexConverter.GetNeighborInDirection(
                    _getCurrentPosition(),
                    command.WorldDirection.Value,
                    _hexConfig)
                : null;

            // Update presenter
            _presenter?.UpdateHighlight(_currentTargetCell);

            OnCommandProcessed?.Invoke(command);

            if (_currentTargetCell.HasValue)
            {
                _logger.Info(LogCategory.Combat,$"[InputCommandHandler] Direction changed to cell: {_currentTargetCell.Value}");
            }
        }

        private void HandleMovementConfirmed(MovementConfirmedCommand command)
        {
            if (!CanProcessInput()) return;
            if (!_isMovementModeActive) return;
            if (!_currentTargetCell.HasValue) return;

            _logger.Info(LogCategory.Combat,$"[InputCommandHandler] Movement confirmed to: {_currentTargetCell.Value}");
            _presenter?.TryMoveToCell(_currentTargetCell.Value);

            OnCommandProcessed?.Invoke(command);
        }

        private void HandleMovementCancelled(MovementCancelledCommand command)
        {
            if (!CanProcessInput()) return;

            // Clear state and highlights
            _isMovementModeActive = false;
            _currentTargetCell = null;
            _presenter?.UpdateHighlight(null);

            OnCommandProcessed?.Invoke(command);
            _logger.Info(LogCategory.Combat,"[InputCommandHandler] Movement cancelled");
        }

        private bool CanProcessInput()
        {
            if (_presenter == null) return false;
            if (_getCurrentPosition == null) return false;
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
