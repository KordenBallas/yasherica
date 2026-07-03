using System.Collections;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Thin adapter that keeps a unit's visual yaw in sync with its domain facing so the
    /// facing is legible on the model (Pillar 4). Reacts to combat state changes only;
    /// no per-frame logic outside the short rotation coroutine.
    /// </summary>
    public class UnitFacingRotator : MonoBehaviour
    {
        private const float RotationDurationSeconds = 0.15f;

        private ICombatController _combatController;
        private IBattlefield _battlefield;
        private HexDirectionConfig _hexConfig;
        private int _unitId;
        private HexDirection _lastFacing;
        private Coroutine _rotationCoroutine;

        public void Initialize(
            ICombatController combatController,
            IBattlefield battlefield,
            HexDirectionConfig hexConfig,
            int unitId)
        {
            _combatController = combatController;
            _battlefield = battlefield;
            _hexConfig = hexConfig;
            _unitId = unitId;

            var unit = combatController.CombatState?.GetUnit(unitId);
            if (unit != null)
            {
                _lastFacing = unit.FacingDirection;
                ApplyYawInstant(unit);
            }

            _combatController.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (_combatController != null)
                _combatController.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ICombatState state)
        {
            var unit = state.GetUnit(_unitId);
            if (unit == null || unit.FacingDirection == _lastFacing)
                return;

            _lastFacing = unit.FacingDirection;

            var lookDirection = WorldLookDirection(unit);
            if (lookDirection == Vector3.zero)
                return;

            if (_rotationCoroutine != null)
                StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = StartCoroutine(RotateTo(Quaternion.LookRotation(lookDirection)));
        }

        private void ApplyYawInstant(IUnit unit)
        {
            var lookDirection = WorldLookDirection(unit);
            if (lookDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        /// <summary>
        /// World-space look direction derived from the grid itself (HexToWorld of the facing
        /// neighbor minus the unit's cell), so any grid orientation stays correct.
        /// </summary>
        private Vector3 WorldLookDirection(IUnit unit)
        {
            var neighbor = FacingGeometry.Neighbor(unit.Position, unit.FacingDirection, _hexConfig);
            var direction = _battlefield.HexToWorld(neighbor) - _battlefield.HexToWorld(unit.Position);
            direction.y = 0f;
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
        }

        private IEnumerator RotateTo(Quaternion target)
        {
            var start = transform.rotation;
            float elapsed = 0f;

            while (elapsed < RotationDurationSeconds)
            {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(start, target, Mathf.Clamp01(elapsed / RotationDurationSeconds));
                yield return null;
            }

            transform.rotation = target;
            _rotationCoroutine = null;
        }
    }
}
