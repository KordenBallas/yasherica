using System;
using Combat.Config;
using Combat.Input.Commands;
using Core.Logging;
using GameInput.Core;
using GameInput.View;
using UnityEngine;
using Zenject;

namespace Combat.Input
{
    /// <summary>
    /// Combat input controller reading the shared named actions (Input Foundation R1), so every
    /// gesture works on keyboard+mouse and gamepad at once — no per-platform controller switching.
    /// Movement: hold MoveMode (M / LT) → aim → release to confirm (Cancel aborts).
    /// Abilities: hold a slot (Q..Y / d-pad+bumpers) → aim → release to confirm (Cancel aborts).
    /// Queue (D7): tap Fire (Enter / RT) to volley along the current facing; hold past the threshold
    /// to aim the whole volley, release to fire (Cancel aborts).
    /// Aiming reads <see cref="IAimDirectionResolver"/> — cursor raycast or left stick by device.
    /// </summary>
    public class CombatInputController : MonoBehaviour, IInputController
    {
        private const int AbilitySlotCount = 6;

        [Inject] private InputConfig _config;
        [Inject] private IGameLogger _logger;
        [Inject] private IGameActions _actions;
        [Inject] private IAimDirectionResolver _aimResolver;
        [SerializeField] private Transform _characterTransform;

        private bool _isEnabled;

        // Movement hold state
        private bool _wasMovementModeActive;

        // Ability hold state
        private int? _heldAbilityIndex;

        // Volley fire/aim state (Fire, D7): a short tap fires the queue immediately along the
        // current facing; holding past the threshold enters aim mode (turn toward the cursor,
        // release to execute). _volleyPressStartTime tracks the press while it is still a "tap".
        private bool _wasVolleyAimActive;
        private float? _volleyPressStartTime;

        // Shared abort flag — set by Cancel while holding any gesture
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
        public event Action<VolleyAimStartedCommand> OnVolleyAimStarted;
        public event Action<VolleyAimCancelledCommand> OnVolleyAimCancelled;

        private void Update()
        {
            if (!_isEnabled) return;

            bool isMovementKeyPressed = IsPressed(GameAction.CombatMoveMode);

            // Movement key transitions
            if (isMovementKeyPressed && !_wasMovementModeActive)
            {
                // Just pressed — enter movement mode only if no ability or volley press is held
                if (!_heldAbilityIndex.HasValue && !_wasVolleyAimActive && !_volleyPressStartTime.HasValue)
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

            // Volley fire/aim (Fire, D7): a short tap discharges the queue immediately along the
            // hero's current facing; holding past the threshold enters aim mode — turn toward the
            // cursor, release to execute (the shipped D2 gesture). Cancel aborts either stage.
            bool isExecuteKeyPressed = IsPressed(GameAction.Fire);
            if (isExecuteKeyPressed && !_wasVolleyAimActive && !_volleyPressStartTime.HasValue)
            {
                // Just pressed — start tracking the press only if nothing else is held
                if (!_heldAbilityIndex.HasValue && !_wasMovementModeActive)
                {
                    _volleyPressStartTime = Time.time;
                    _aimAborted = false;
                }
            }
            else if (isExecuteKeyPressed && _volleyPressStartTime.HasValue && !_wasVolleyAimActive)
            {
                // Still held — crossing the threshold turns the tap into an aim hold
                if (!_aimAborted && Time.time - _volleyPressStartTime.Value >= _config.volleyAimHoldThresholdSeconds)
                {
                    _wasVolleyAimActive = true;
                    OnVolleyAimStarted?.Invoke(new VolleyAimStartedCommand(Time.time));
                }
            }
            else if (!isExecuteKeyPressed && (_wasVolleyAimActive || _volleyPressStartTime.HasValue))
            {
                // Released — a tap fires along the current facing, an aim hold fires along the
                // aimed facing; both skip firing if Cancel aborted the gesture.
                if (!_aimAborted)
                    OnExecuteQueueRequested?.Invoke(new ExecuteQueueCommand(true));

                _wasVolleyAimActive = false;
                _volleyPressStartTime = null;
                _lastDirection = null;
            }

            // Cancel (right-click / Esc / gamepad east) — checked before direction so abort is set first
            if (WasPressedThisFrame(GameAction.Cancel))
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
                else if (_wasVolleyAimActive || _volleyPressStartTime.HasValue)
                {
                    // Cancels the aim hold and a not-yet-threshold tap alike (D7).
                    if (_wasVolleyAimActive)
                        OnVolleyAimCancelled?.Invoke(new VolleyAimCancelledCommand(Time.time));
                    _aimAborted = true;
                }
            }

            // Direction updates while movement, ability, or volley-aim mode is active
            if (_wasMovementModeActive || _heldAbilityIndex.HasValue || _wasVolleyAimActive)
            {
                var currentDirection = _aimResolver.ResolveAimDirection(_characterTransform);
                if (!DirectionsEqual(currentDirection, _lastDirection))
                {
                    _lastDirection = currentDirection;
                    OnMovementDirectionChanged?.Invoke(
                        new MovementDirectionChangedCommand(currentDirection, Time.time));
                }
            }

            // Ability slot press — only when nothing else is held
            if (!_heldAbilityIndex.HasValue && !_wasMovementModeActive && !_wasVolleyAimActive && !_volleyPressStartTime.HasValue)
            {
                for (int i = 0; i < AbilitySlotCount; i++)
                {
                    if (WasPressedThisFrame(GameAction.AbilitySlot1 + i))
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
                // Ability slot release — confirm or discard
                if (WasReleasedThisFrame(GameAction.AbilitySlot1 + _heldAbilityIndex.Value))
                {
                    if (!_aimAborted)
                        OnAbilityConfirmed?.Invoke(new AbilityConfirmedCommand(Time.time));

                    _heldAbilityIndex = null;
                    _lastDirection = null;
                }
            }

            // Fire is a tap-to-fire / hold-to-aim gesture (handled above), not a single press (D7).

            // Change direction
            if (WasPressedThisFrame(GameAction.CombatChangeDirection))
                OnChangeDirectionRequested?.Invoke(new ChangeDirectionModeCommand(true));
        }

        private bool DirectionsEqual(Vector3? a, Vector3? b)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return Vector3.Distance(a.Value, b.Value) < 0.01f;
        }

        // Legacy properties
        [Obsolete("Use OnMovementModeChanged event instead.")]
        public bool IsMovementModeActive => _isEnabled && IsPressed(GameAction.CombatMoveMode);

        [Obsolete("Use OnMovementDirectionChanged event instead.")]
        public Vector3? GetMovementDirection()
        {
#pragma warning disable CS0618
            if (!IsMovementModeActive) return null;
#pragma warning restore CS0618
            return _aimResolver.ResolveAimDirection(_characterTransform);
        }

        [Obsolete("Use OnMovementConfirmed event instead.")]
        public bool IsConfirmPressed =>
            _isEnabled && WasPressedThisFrame(GameAction.Confirm);

        [Obsolete("Use OnMovementCancelled event instead.")]
        public bool IsCancelPressed =>
            _isEnabled && WasPressedThisFrame(GameAction.Cancel);

        public void Enable()
        {
            _isEnabled = true;
            _wasMovementModeActive = false;
            _heldAbilityIndex = null;
            _wasVolleyAimActive = false;
            _volleyPressStartTime = null;
            _aimAborted = false;
            _lastDirection = null;
            _logger.Info(LogCategory.Combat, "[CombatInputController] Enabled");
        }

        public void Disable()
        {
            if (_wasMovementModeActive)
                OnMovementModeChanged?.Invoke(new MovementModeChangedCommand(false, Time.time));

            _isEnabled = false;
            _wasMovementModeActive = false;
            _heldAbilityIndex = null;
            _wasVolleyAimActive = false;
            _volleyPressStartTime = null;
            _aimAborted = false;
            _lastDirection = null;
            _logger.Info(LogCategory.Combat, "[CombatInputController] Disabled");
        }

        public void SetCharacterTransform(Transform characterTransform)
        {
            _characterTransform = characterTransform;
            _logger.Info(LogCategory.Combat, $"[CombatInputController] SetCharacterTransform: {characterTransform?.name ?? "null"}");
        }

        private bool IsPressed(GameAction action)
        {
            var inputAction = _actions.Get(action);
            return inputAction != null && inputAction.IsPressed();
        }

        private bool WasPressedThisFrame(GameAction action)
        {
            var inputAction = _actions.Get(action);
            return inputAction != null && inputAction.WasPressedThisFrame();
        }

        private bool WasReleasedThisFrame(GameAction action)
        {
            var inputAction = _actions.Get(action);
            return inputAction != null && inputAction.WasReleasedThisFrame();
        }
    }
}
