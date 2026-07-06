using System.Collections;
using System.Collections.Generic;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Integration
{
    /// <summary>
    /// The one visual-sync path every combat unit rides (D6): wraps the internal immutable
    /// <see cref="Unit"/>, mirrors state changes onto the GameObject, glides cell-to-cell instead
    /// of teleporting (D8), and clears the model from the board the moment the unit dies (D5 —
    /// instant removal is the placeholder; a death animation replaces it later). Hero and enemy
    /// adapters are thin subclasses — they differ only in how their Unit is built and in what
    /// death looks like. MonoBehaviour adapter: no business logic, sync only.
    /// </summary>
    public abstract class CombatUnitComponentBase : MonoBehaviour, IUnit
    {
        // A gliding model briefly turns into its direction of travel, then turns back to its
        // domain facing on arrival — facing legibility (R8) holds while the walk reads face-first.
        private const float WalkTurnSeconds = 0.12f;
        private const float MinTurnAngleDegrees = 1f;

        private Unit _internalUnit;
        private ICombatController _combatController;
        private float _feetOffset;
        private CombatMovementConfig _movementConfig;
        private ICharacterMovementAnimator _moveAnimator;
        private HexDirectionConfig _hexConfig;
        private Coroutine _moveRoutine;
        private bool _deathHandled;
        [Inject] private IGameLogger _logger;

        protected IGameLogger Logger => _logger;
        protected virtual LogCategory LogCategory => LogCategory.Combat;

        // IUnitIdentity
        public int Id => _internalUnit?.Id ?? -1;
        public IPlayer Owner => _internalUnit?.Owner;

        // IUnitPosition
        public HexCoordinates Position => _internalUnit?.Position ?? new HexCoordinates(0, 0);
        public HexDirection FacingDirection => _internalUnit?.FacingDirection ?? HexDirection.E;

        // IUnitHealth
        public int CurrentHP => _internalUnit?.CurrentHP ?? 0;
        public int MaxHP => _internalUnit?.MaxHP ?? 0;
        public bool IsAlive => _internalUnit?.IsAlive ?? false;

        // IUnitCombatant
        public IReadOnlyList<IAbilityInstance> Abilities => _internalUnit?.Abilities ?? new List<IAbilityInstance>();
        public IReadOnlyList<ScheduledAbility> AbilityQueue => _internalUnit?.AbilityQueue ?? new List<ScheduledAbility>();
        public IReadOnlyList<IStatusEffect> StatusEffects => _internalUnit?.StatusEffects ?? new List<IStatusEffect>();

        // IUnitActionState
        public bool HasActedThisTurn => _internalUnit?.HasActedThisTurn ?? false;
        public bool CanAct => _internalUnit?.CanAct ?? false;
        public UnitActionState ActionState => _internalUnit?.ActionState ?? UnitActionState.Dead;

        /// <summary>True once the internal Unit exists (the component went through combat init).</summary>
        public bool IsInitializedForCombat => _internalUnit != null;

        /// <summary>
        /// Provides access to the internal Unit for registration with CombatState.
        /// The pure C# Unit is what enters the state — never the MonoBehaviour adapter.
        /// </summary>
        public IUnit InternalUnit => _internalUnit;

        // IUnitCombatant methods
        public IAbilityInstance GetAbility(int abilityId) => _internalUnit?.GetAbility(abilityId);
        public IReadOnlyList<IAbilityInstance> GetAvailableAbilities() => _internalUnit?.GetAvailableAbilities() ?? new List<IAbilityInstance>();
        public bool CanScheduleAbility() => _internalUnit?.CanScheduleAbility() ?? false;
        public bool CanMove() => _internalUnit?.CanMove() ?? false;

        /// <summary>
        /// Wires the freshly built internal Unit to the combat state stream. Movement config +
        /// animator are optional: when present, board moves glide cell-to-cell; when absent
        /// (e.g. a caller that has no animation seam yet), position changes snap as before.
        /// </summary>
        protected void BeginCombat(
            Unit internalUnit,
            ICombatController combatController,
            CombatMovementConfig movementConfig = null,
            ICharacterMovementAnimator moveAnimator = null,
            HexDirectionConfig hexConfig = null)
        {
            _internalUnit = internalUnit;
            _combatController = combatController;
            _movementConfig = movementConfig;
            _moveAnimator = moveAnimator;
            _hexConfig = hexConfig;
            _feetOffset = UnitGrounding.FeetOffsetFor(transform);
            _deathHandled = false;

            _combatController.OnStateChanged += OnCombatStateChanged;
        }

        /// <summary>
        /// What removal from the board looks like for this unit kind. Default: the model
        /// disappears (D5 placeholder for a death animation). The GameObject itself is never
        /// destroyed here — post-combat consumers (loot drop position, platform cleanup) still
        /// read it.
        /// </summary>
        protected virtual void OnUnitDied()
        {
            gameObject.SetActive(false);
        }

        private void OnCombatStateChanged(ICombatState newState)
        {
            if (_internalUnit == null)
                return;

            var updatedUnit = newState.GetUnit(_internalUnit.Id);
            if (updatedUnit == null)
            {
                _logger?.Warning(LogCategory, $"[{GetType().Name}] Unit {_internalUnit.Id} not found in updated state");
                return;
            }

            var newUnit = updatedUnit as Unit;
            if (newUnit == null)
            {
                _logger?.Error(LogCategory, $"[{GetType().Name}] State contains non-Unit IUnit: {updatedUnit.GetType().Name}");
                return;
            }

            bool positionChanged = !newUnit.Position.Equals(_internalUnit.Position);
            _internalUnit = newUnit;

            if (!newUnit.IsAlive && !_deathHandled)
            {
                _deathHandled = true;
                if (_moveRoutine != null)
                {
                    StopCoroutine(_moveRoutine);
                    _moveRoutine = null;
                }

                _logger?.Info(LogCategory, $"[{GetType().Name}] Unit {Id} died — cleared from the board");
                OnUnitDied();
                return;
            }

            // Reposition only when the cell actually changed: re-snapping on every state change
            // fought the additive wind-up pose and read as a recoil (D8).
            if (positionChanged && _combatController?.Battlefield != null)
            {
                Vector3 worldPosition = UnitGrounding.Grounded(
                    _combatController.Battlefield.HexToWorld(Position), _feetOffset);
                MoveVisualTo(worldPosition);
                _logger?.Info(LogCategory, $"[{GetType().Name}] Unit {Id} → {Position} ({worldPosition}), HP={CurrentHP}/{MaxHP}");
            }
        }

        /// <summary>
        /// Glides the model to its new cell (interrupting any glide in progress from wherever
        /// the model currently is), or snaps when no animation seam was provided / the object
        /// cannot run coroutines.
        /// </summary>
        private void MoveVisualTo(Vector3 worldPosition)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            if (_moveAnimator != null && _movementConfig != null && isActiveAndEnabled)
            {
                _moveRoutine = StartCoroutine(GlideTo(worldPosition));
            }
            else
            {
                transform.position = worldPosition;
            }
        }

        private IEnumerator GlideTo(Vector3 worldPosition)
        {
            // Face the travel direction for the walk, then face the domain facing again on
            // arrival: the model never walks back-first, and its resting orientation stays the
            // honest volley pointer (R8). For an enemy whose facing already IS its move
            // direction both turns are no-ops.
            var walk = worldPosition - transform.position;
            walk.y = 0f;
            if (walk.sqrMagnitude > 0.0001f)
                yield return TurnTo(Quaternion.LookRotation(walk.normalized));

            yield return _moveAnimator.AnimateMovement(transform, transform.position, worldPosition);

            var facingLook = DomainFacingLookDirection();
            if (facingLook != Vector3.zero)
                yield return TurnTo(Quaternion.LookRotation(facingLook));

            _moveRoutine = null;
        }

        private IEnumerator TurnTo(Quaternion target)
        {
            if (Quaternion.Angle(transform.rotation, target) < MinTurnAngleDegrees)
                yield break;

            var start = transform.rotation;
            float elapsed = 0f;
            while (elapsed < WalkTurnSeconds)
            {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(start, target, Mathf.Clamp01(elapsed / WalkTurnSeconds));
                yield return null;
            }

            transform.rotation = target;
        }

        /// <summary>
        /// The world direction the unit's domain facing points at — the same grid-derived look
        /// <c>UnitFacingRotator</c> uses, so the post-walk restore and the rotator always agree.
        /// Zero when the facing seam was not provided (no hex config) or geometry degenerates.
        /// </summary>
        private Vector3 DomainFacingLookDirection()
        {
            if (_hexConfig == null || _internalUnit == null || _combatController?.Battlefield == null)
                return Vector3.zero;

            var battlefield = _combatController.Battlefield;
            var neighbor = FacingGeometry.Neighbor(Position, FacingDirection, _hexConfig);
            var direction = battlefield.HexToWorld(neighbor) - battlefield.HexToWorld(Position);
            direction.y = 0f;
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
        }

        private void OnDestroy()
        {
            if (_combatController != null)
            {
                _combatController.OnStateChanged -= OnCombatStateChanged;
                _logger?.Info(LogCategory, $"[{GetType().Name}] Unit {Id} destroyed and unsubscribed from OnStateChanged");
            }
        }
    }
}
