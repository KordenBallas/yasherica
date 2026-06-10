using System;
using Combat.Config;
using Combat.Input.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// Joystick/gamepad input controller.
    /// Uses Unity's new Input System for gamepad support.
    /// Emits commands via events when input state changes.
    /// </summary>
    public class JoystickInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;

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

        private void Update()
        {
            if (!_isEnabled) return;
            if (Gamepad.current == null) return;

            // Check if joystick has meaningful input (movement mode proxy)
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            bool hasInput = leftStick.magnitude > _config.inputDeadzone;

            // Detect movement mode change
            if (hasInput != _wasMovementModeActive)
            {
                _wasMovementModeActive = hasInput;
                OnMovementModeChanged?.Invoke(
                    new MovementModeChangedCommand(hasInput, Time.time));

                // Clear direction when movement mode deactivates
                if (!hasInput)
                {
                    _lastDirection = null;
                }
            }

            // Direction changes (only while movement mode is active)
            if (hasInput)
            {
                var direction = new Vector3(leftStick.x, 0, leftStick.y).normalized;
                if (!DirectionsEqual(direction, _lastDirection))
                {
                    _lastDirection = direction;
                    OnMovementDirectionChanged?.Invoke(
                        new MovementDirectionChangedCommand(direction, Time.time));
                }
            }

            // Confirm button (A on Xbox, X on PlayStation)
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                OnMovementConfirmed?.Invoke(new MovementConfirmedCommand(Time.time));
            }

            // Cancel button (B on Xbox, Circle on PlayStation)
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                OnMovementCancelled?.Invoke(new MovementCancelledCommand(Time.time));
            }
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
        public bool IsMovementModeActive => _isEnabled;

        [Obsolete("Use OnMovementDirectionChanged event instead.")]
        public Vector3? GetMovementDirection()
        {
            if (!_isEnabled) return null;

            if (Gamepad.current == null)
                return null;

            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            float h = leftStick.x;
            float v = leftStick.y;

            if (Mathf.Abs(h) < _config.inputDeadzone && Mathf.Abs(v) < _config.inputDeadzone)
                return null;

            return new Vector3(h, 0, v).normalized;
        }

        [Obsolete("Use OnMovementConfirmed event instead.")]
        public bool IsConfirmPressed =>
            _isEnabled && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        [Obsolete("Use OnMovementCancelled event instead.")]
        public bool IsCancelPressed =>
            _isEnabled && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        public void Enable()
        {
            _isEnabled = true;
            _wasMovementModeActive = false;
            _lastDirection = null;
            Debug.Log("[JoystickInputController] Enabled");
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
            Debug.Log("[JoystickInputController] Disabled");
        }
    }
}
