using System;
using Combat.Config;
using Combat.Input.Commands;
using Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// Mobile input controller using touch input.
    /// Uses Unity's new Input System for touch support.
    /// Emits commands via events when input state changes.
    /// </summary>
    public class MobileInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;
        [Inject] private IGameLogger _logger;

        private Vector3? _dragDirection;
        private bool _isEnabled;

        // State tracking for change detection
        private bool _wasMovementModeActive;
        private Vector3? _lastDirection;

        // Events
        public event Action<MovementModeChangedCommand> OnMovementModeChanged;
        public event Action<MovementDirectionChangedCommand> OnMovementDirectionChanged;
        public event Action<MovementConfirmedCommand> OnMovementConfirmed;
        public event Action<MovementCancelledCommand> OnMovementCancelled;
        public event Action<AbilitySelectedCommand> OnAbilitySelected;
        public event Action<AbilityCancelledCommand> OnAbilityCancelled;
#pragma warning disable 67
        public event Action<AbilityConfirmedCommand> OnAbilityConfirmed;
#pragma warning restore 67
        public event Action<ExecuteQueueCommand> OnExecuteQueueRequested;
        public event Action<ChangeDirectionModeCommand> OnChangeDirectionRequested;

        private void Awake()
        {
            // Enable enhanced touch support for the new Input System
            EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            if (!_isEnabled) return;
            if (Touchscreen.current == null) return;

            var touches = Touchscreen.current.touches;
            bool hasTouches = touches.Count > 0;

            // Detect movement mode change (touch active)
            if (hasTouches != _wasMovementModeActive)
            {
                _wasMovementModeActive = hasTouches;
                OnMovementModeChanged?.Invoke(
                    new MovementModeChangedCommand(hasTouches, Time.time));

                // Clear direction when no touches
                if (!hasTouches)
                {
                    _lastDirection = null;
                }
            }

            // Process touch input
            if (hasTouches)
            {
                var touch = touches[0];
                var phase = touch.phase.ReadValue();

                // Direction changes (from drag)
                var currentDirection = _dragDirection; // TODO: Calculate from touch drag
                if (!DirectionsEqual(currentDirection, _lastDirection))
                {
                    _lastDirection = currentDirection;
                    OnMovementDirectionChanged?.Invoke(
                        new MovementDirectionChangedCommand(currentDirection, Time.time));
                }

                // Confirm on touch end (tap)
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    OnMovementConfirmed?.Invoke(new MovementConfirmedCommand(Time.time));
                }
            }

            // TODO: Implement cancel gesture (e.g., two-finger tap)
        }

        /// <summary>
        /// Compares two direction vectors with tolerance.
        /// </summary>
        private bool DirectionsEqual(Vector3? a, Vector3? b)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return Vector3.Distance(a.Value, b.Value) < 0.01f;
        }

        // Legacy properties (deprecated but functional for backwards compatibility)
        [Obsolete("Use OnMovementModeChanged event instead.")]
        public bool IsMovementModeActive => _isEnabled && Touchscreen.current != null && Touchscreen.current.touches.Count > 0;

        [Obsolete("Use OnMovementDirectionChanged event instead.")]
        public Vector3? GetMovementDirection()
        {
#pragma warning disable CS0618 // Suppress obsolete warning for internal use
            if (!IsMovementModeActive) return null;
#pragma warning restore CS0618

            // TODO: Implement touch drag direction detection
            return _dragDirection;
        }

        [Obsolete("Use OnMovementConfirmed event instead.")]
        public bool IsConfirmPressed
        {
            get
            {
                if (!_isEnabled || Touchscreen.current == null)
                    return false;

                var touches = Touchscreen.current.touches;
                if (touches.Count == 0)
                    return false;

                var touch = touches[0];
                return touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended;
            }
        }

        [Obsolete("Use OnMovementCancelled event instead.")]
        public bool IsCancelPressed => false; // TODO: Implement

        public void Enable()
        {
            _isEnabled = true;
            _wasMovementModeActive = false;
            _lastDirection = null;
            _logger.Info(LogCategory.Combat,"[MobileInputController] Enabled");
        }

        public void Disable()
        {
            // Emit deactivation if movement was active
            if (_wasMovementModeActive)
            {
                OnMovementModeChanged?.Invoke(
                    new MovementModeChangedCommand(false, Time.time));
            }

            _isEnabled = false;
            _wasMovementModeActive = false;
            _lastDirection = null;
            _logger.Info(LogCategory.Combat,"[MobileInputController] Disabled");
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
        }
    }
}
