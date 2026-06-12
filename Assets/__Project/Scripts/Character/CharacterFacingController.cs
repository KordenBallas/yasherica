using System.Collections;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// MonoBehaviour adapter on the hero root implementing ICharacterFacing:
    /// smooth yaw-only rotation toward a target point and restoration of the
    /// facing the character had before the first turn. Movement input is expected
    /// to be locked while a forced facing is active.
    /// </summary>
    public class CharacterFacingController : MonoBehaviour, ICharacterFacing
    {
        private const float MinimumDirectionSqrMagnitude = 1e-6f;

        private Quaternion _storedRotation;
        private bool _hasStoredRotation;
        private Coroutine _rotationCoroutine;

        public void FaceTowards(Vector3 worldPoint, float duration)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return;
            }

            // Only the first turn stores the facing, so repeated FaceTowards calls
            // before a restore still return to the original orientation.
            if (!_hasStoredRotation)
            {
                _storedRotation = transform.rotation;
                _hasStoredRotation = true;
            }

            StartRotation(Quaternion.LookRotation(direction), duration);
        }

        public void RestoreFacing(float duration)
        {
            if (!_hasStoredRotation)
            {
                return;
            }

            _hasStoredRotation = false;
            StartRotation(_storedRotation, duration);
        }

        private void StartRotation(Quaternion target, float duration)
        {
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }

            _rotationCoroutine = StartCoroutine(RotateTo(target, duration));
        }

        private IEnumerator RotateTo(Quaternion target, float duration)
        {
            if (duration <= 0f)
            {
                transform.rotation = target;
                _rotationCoroutine = null;
                yield break;
            }

            Quaternion start = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth step matches the easing style used by the camera transitions.
                t = t * t * (3f - 2f * t);
                transform.rotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            transform.rotation = target;
            _rotationCoroutine = null;
        }
    }
}
