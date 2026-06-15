using System.Collections;
using Inventory.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// Adapter on the inventory stage root. The stage GameObjects stay alive so
    /// coroutines and particles keep their state; only the overlay camera toggles,
    /// which both hides the diorama and disables its PhysicsRaycaster. Also reframes
    /// the camera between crafting (slots above the pot) and feeding (slots below it).
    /// </summary>
    public class InventoryStageView : MonoBehaviour, IInventoryStageView
    {
        [Header("Scene References")]
        [Tooltip("The stage's overlay camera (URP Render Type = Overlay)")]
        [SerializeField] private UnityEngine.Camera _stageCamera;

        [Inject] private InventoryConfig _config;

        private Vector3 _defaultCameraLocalPosition;
        private Coroutine _framingCoroutine;

        private void Awake()
        {
            if (_stageCamera != null)
            {
                _defaultCameraLocalPosition = _stageCamera.transform.localPosition;
            }

            // The stage starts hidden; the presenter activates it on open.
            SetStageActive(false);
        }

        public void SetStageActive(bool active)
        {
            if (_stageCamera == null)
            {
                Debug.LogError("[InventoryStageView] Stage camera is not assigned!");
                return;
            }

            _stageCamera.enabled = active;
        }

        public void SetFeedingFraming(bool feeding)
        {
            if (_stageCamera == null)
            {
                return;
            }

            // Feeding lowers the framing by the configured drop so the slots below the
            // pot come into view; crafting returns to the default position.
            float drop = _config != null ? _config.FeedingCameraDrop : 0f;
            Vector3 target = feeding
                ? _defaultCameraLocalPosition + Vector3.down * drop
                : _defaultCameraLocalPosition;

            if (_framingCoroutine != null)
            {
                StopCoroutine(_framingCoroutine);
            }

            _framingCoroutine = StartCoroutine(MoveCamera(target));
        }

        private IEnumerator MoveCamera(Vector3 target)
        {
            float duration = _config != null ? _config.FeedingFramingDuration : 0f;
            Vector3 start = _stageCamera.transform.localPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth step matches the easing used by the other stage animations.
                t = t * t * (3f - 2f * t);
                _stageCamera.transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            _stageCamera.transform.localPosition = target;
            _framingCoroutine = null;
        }
    }
}
