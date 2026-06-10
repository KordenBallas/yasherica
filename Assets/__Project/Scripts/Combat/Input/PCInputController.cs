using System;
using Combat.Config;
using Combat.Input.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// PC input controller using mouse and keyboard.
    /// Movement: hold M → aim with mouse → release M to confirm (right-click aborts).
    /// Abilities: hold Q/W/E/R/T/Y → aim with mouse → release to confirm (right-click aborts).
    /// </summary>
    public class PCInputController : MonoBehaviour, IInputController
    {
        [Inject] private InputConfig _config;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _characterTransform;

        private bool _isEnabled;
        private float _lastLogTime;
        private const float LOG_INTERVAL = 0.5f;

        // Movement hold state
        private bool _wasMovementModeActive;

        // Ability hold state
        private int? _heldAbilityIndex;

        // Shared abort flag — set by right-click while holding any key
        private bool _aimAborted;

        // Direction change detection
        private Vector3? _lastDirection;

        // Events
        public event Action<MovementModeChangedCommand> OnMovementModeChanged;
        public event Action<MovementDirectionChangedCommand> OnMovementDirectionChanged;
        public event Action<MovementConfirmedCommand> OnMovementConfirmed;
        public event Action<MovementCancelledCommand> OnMovementCancelled;
        public event Action<AbilitySelectedCommand> OnAbilitySelected;
        public event Action<AbilityCancelledCommand> OnAbilityCancelled;
        public event Action<AbilityConfirmedCommand> OnAbilityConfirmed;
        public event Action<ExecuteQueueCommand> OnExecuteQueueRequested;
        public event Action<ChangeDirectionModeCommand> OnChangeDirectionRequested;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            if (!_isEnabled) return;

            bool isMovementKeyPressed = IsKeyPressed(_config.movementModeKey);

            // Movement key transitions
            if (isMovementKeyPressed && !_wasMovementModeActive)
            {
                // Just pressed — enter movement mode only if no ability is held
                if (!_heldAbilityIndex.HasValue)
                {
                    _wasMovementModeActive = true;
                    _aimAborted = false;
                    OnMovementModeChanged?.Invoke(new MovementModeChangedCommand(true, Time.time));
                }
            }
            else if (!isMovementKeyPressed && _wasMovementModeActive)
            {
                // Just released — confirm before deactivating so handler still sees mode active
                if (!_aimAborted)
                    OnMovementConfirmed?.Invoke(new MovementConfirmedCommand(Time.time));

                _wasMovementModeActive = false;
                _lastDirection = null;
                OnMovementModeChanged?.Invoke(new MovementModeChangedCommand(false, Time.time));
            }

            // Right-click cancellation (checked before direction so abort is set first)
            if (IsKeyPressedThisFrame(_config.cancelKey))
            {
                if (_wasMovementModeActive)
                {
                    OnMovementCancelled?.Invoke(new MovementCancelledCommand(Time.time));
                    _aimAborted = true;
                }
                else if (_heldAbilityIndex.HasValue)
                {
                    OnAbilityCancelled?.Invoke(new AbilityCancelledCommand(false));
                    _aimAborted = true;
                }
            }

            // Direction updates while movement or ability mode is active
            if (_wasMovementModeActive || _heldAbilityIndex.HasValue)
            {
                var currentDirection = CalculateMovementDirection();
                if (!DirectionsEqual(currentDirection, _lastDirection))
                {
                    _lastDirection = currentDirection;
                    OnMovementDirectionChanged?.Invoke(
                        new MovementDirectionChangedCommand(currentDirection, Time.time));
                }
            }

            // Ability key press — only when nothing else is held
            if (!_heldAbilityIndex.HasValue && !_wasMovementModeActive)
            {
                for (int i = 0; i < _config.abilityKeys.Count; i++)
                {
                    if (IsKeyPressedThisFrame(_config.abilityKeys[i]))
                    {
                        _heldAbilityIndex = i;
                        _aimAborted = false;
                        OnAbilitySelected?.Invoke(new AbilitySelectedCommand(i));
                        break;
                    }
                }
            }
            else if (_heldAbilityIndex.HasValue)
            {
                // Ability key release — confirm or discard
                if (IsKeyReleasedThisFrame(_config.abilityKeys[_heldAbilityIndex.Value]))
                {
                    if (!_aimAborted)
                        OnAbilityConfirmed?.Invoke(new AbilityConfirmedCommand(Time.time));

                    _heldAbilityIndex = null;
                    _lastDirection = null;
                }
            }

            // Enter key — execute queue
            if (IsKeyPressedThisFrame(_config.executeQueueKey))
                OnExecuteQueueRequested?.Invoke(new ExecuteQueueCommand(true));

            // S key — change direction
            if (IsKeyPressedThisFrame(_config.changeDirectionKey))
                OnChangeDirectionRequested?.Invoke(new ChangeDirectionModeCommand(true));
        }

        private Vector3? CalculateMovementDirection()
        {
            if (_characterTransform == null || Mouse.current == null)
                return null;

            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, _characterTransform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 direction = worldPoint - _characterTransform.position;
                direction.y = 0;

                if (direction.magnitude > _config.inputDeadzone)
                    return direction.normalized;
            }

            return null;
        }

        private bool DirectionsEqual(Vector3? a, Vector3? b)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return Vector3.Distance(a.Value, b.Value) < 0.01f;
        }

        // Legacy properties
        [Obsolete("Use OnMovementModeChanged event instead.")]
        public bool IsMovementModeActive => _isEnabled && IsKeyPressed(_config.movementModeKey);

        [Obsolete("Use OnMovementDirectionChanged event instead.")]
        public Vector3? GetMovementDirection()
        {
#pragma warning disable CS0618
            if (!IsMovementModeActive) return null;
#pragma warning restore CS0618
            return CalculateMovementDirection();
        }

        [Obsolete("Use OnMovementConfirmed event instead.")]
        public bool IsConfirmPressed =>
            _isEnabled && IsKeyPressedThisFrame(_config.confirmKey);

        [Obsolete("Use OnMovementCancelled event instead.")]
        public bool IsCancelPressed =>
            _isEnabled && IsKeyPressedThisFrame(_config.cancelKey);

        public void Enable()
        {
            _isEnabled = true;
            _wasMovementModeActive = false;
            _heldAbilityIndex = null;
            _aimAborted = false;
            _lastDirection = null;
            Debug.Log($"[PCInputController] Enabled (Movement: {_config.movementModeKey})");
        }

        public void Disable()
        {
            if (_wasMovementModeActive)
                OnMovementModeChanged?.Invoke(new MovementModeChangedCommand(false, Time.time));

            _isEnabled = false;
            _wasMovementModeActive = false;
            _heldAbilityIndex = null;
            _aimAborted = false;
            _lastDirection = null;
            Debug.Log("[PCInputController] Disabled");
        }

        public void SetCharacterTransform(Transform characterTransform)
        {
            _characterTransform = characterTransform;
            Debug.Log($"[PCInputController] SetCharacterTransform: {characterTransform?.name ?? "null"}");
        }

        private bool IsKeyPressed(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Mouse0)
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (keyCode == KeyCode.Mouse1)
                return Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (keyCode == KeyCode.Mouse2)
                return Mouse.current != null && Mouse.current.middleButton.isPressed;

            if (Keyboard.current == null) return false;
            Key key = ConvertKeyCodeToKey(keyCode);
            return key != Key.None && Keyboard.current[key].isPressed;
        }

        private bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Mouse0)
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (keyCode == KeyCode.Mouse1)
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            if (keyCode == KeyCode.Mouse2)
                return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;

            if (Keyboard.current == null) return false;
            Key key = ConvertKeyCodeToKey(keyCode);
            return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
        }

        private bool IsKeyReleasedThisFrame(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Mouse0)
                return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            if (keyCode == KeyCode.Mouse1)
                return Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
            if (keyCode == KeyCode.Mouse2)
                return Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;

            if (Keyboard.current == null) return false;
            Key key = ConvertKeyCodeToKey(keyCode);
            return key != Key.None && Keyboard.current[key].wasReleasedThisFrame;
        }

        private Key ConvertKeyCodeToKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                default:
                    Debug.LogWarning($"[PCInputController] KeyCode {keyCode} not mapped");
                    return Key.None;
            }
        }
    }
}
