using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace Core.Camera
{
    /// <summary>
    /// MonoBehaviour service for managing camera transitions.
    /// Handles switching between isometric and combat cameras using Cinemachine priorities
    /// and smooth rotation interpolation.
    /// </summary>
    public class CameraService : MonoBehaviour, ICameraService
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineCamera _isometricCamera;
        [SerializeField] private CinemachineCamera _combatCamera;
        [SerializeField] private CinemachineCamera _bellyCamera;

        [Header("Configuration")]
        [SerializeField] private CameraConfig _config;

        [Header("Output")]
        [Tooltip("The camera the Cinemachine brain renders through (Main Camera)")]
        [SerializeField] private Transform _outputCameraTransform;

        private const int ISOMETRIC_PRIORITY_ACTIVE = 10;
        private const int ISOMETRIC_PRIORITY_INACTIVE = 0;
        private const int COMBAT_PRIORITY_ACTIVE = 20;
        private const int COMBAT_PRIORITY_INACTIVE = 0;
        private const int BELLY_PRIORITY_ACTIVE = 30;
        private const int BELLY_PRIORITY_INACTIVE = 0;

        private enum CameraView
        {
            Isometric,
            Combat,
            Belly
        }

        private bool _isTransitioning;
        private Coroutine _rotationTransitionCoroutine;
        private Transform _bellyAnchor;
        private CameraView _activeView = CameraView.Isometric;
        private CameraView _viewBeforeBelly = CameraView.Isometric;

        public bool IsTransitioning => _isTransitioning;

        public Vector3 OutputCameraPosition
        {
            get
            {
                if (_outputCameraTransform == null)
                {
                    Debug.LogError("[CameraService] Output camera transform is not assigned!");
                    return Vector3.zero;
                }

                return _outputCameraTransform.position;
            }
        }

        private void Start()
        {
            // Ensure isometric camera is active by default
            if (_isometricCamera != null)
            {
                _isometricCamera.Priority = ISOMETRIC_PRIORITY_ACTIVE;
            }
            
            if (_combatCamera != null)
            {
                _combatCamera.Priority = COMBAT_PRIORITY_INACTIVE;
                ApplyCombatCameraSettings();
            }

            if (_bellyCamera != null)
            {
                _bellyCamera.Priority = BELLY_PRIORITY_INACTIVE;
            }

            if (_config == null)
            {
                Debug.LogWarning("[CameraService] CameraConfig is not assigned. Using default values.");
            }
        }
        
        public void SwitchToCombatCamera(float transitionTime = -1f)
        {
            if (_combatCamera == null)
            {
                Debug.LogError("[CameraService] Combat camera is not assigned!");
                return;
            }
            
            float duration = transitionTime >= 0 ? transitionTime : _config?.TransitionTime ?? 2f;
            Vector3 targetRotation = _config?.CombatRotation ?? new Vector3(45f, 90f, 0f);
            
            // Switch camera priorities
            if (_isometricCamera != null)
            {
                _isometricCamera.Priority = ISOMETRIC_PRIORITY_INACTIVE;
            }
            if (_bellyCamera != null)
            {
                _bellyCamera.Priority = BELLY_PRIORITY_INACTIVE;
            }
            _combatCamera.Priority = COMBAT_PRIORITY_ACTIVE;
            _activeView = CameraView.Combat;
            
            // Start smooth rotation transition
            if (_rotationTransitionCoroutine != null)
            {
                StopCoroutine(_rotationTransitionCoroutine);
            }
            _rotationTransitionCoroutine = StartCoroutine(TransitionRotation(_combatCamera, targetRotation, duration));
            
            Debug.Log($"[CameraService] Switching to combat camera (rotation: {targetRotation}, time: {duration}s)");
        }
        
        public void SwitchToIsometricCamera(float transitionTime = -1f)
        {
            if (_isometricCamera == null)
            {
                Debug.LogError("[CameraService] Isometric camera is not assigned!");
                return;
            }
            
            float duration = transitionTime >= 0 ? transitionTime : _config?.TransitionTime ?? 2f;
            Vector3 targetRotation = _config?.IsometricRotation ?? new Vector3(30f, 45f, 0f);
            
            // Switch camera priorities
            if (_combatCamera != null)
            {
                _combatCamera.Priority = COMBAT_PRIORITY_INACTIVE;
            }
            if (_bellyCamera != null)
            {
                _bellyCamera.Priority = BELLY_PRIORITY_INACTIVE;
            }
            _isometricCamera.Priority = ISOMETRIC_PRIORITY_ACTIVE;
            _activeView = CameraView.Isometric;
            
            // Start smooth rotation transition
            if (_rotationTransitionCoroutine != null)
            {
                StopCoroutine(_rotationTransitionCoroutine);
            }
            _rotationTransitionCoroutine = StartCoroutine(TransitionRotation(_isometricCamera, targetRotation, duration));
            
            Debug.Log($"[CameraService] Switching to isometric camera (rotation: {targetRotation}, time: {duration}s)");
        }
        
        public void SetBellyAnchor(Transform anchor)
        {
            _bellyAnchor = anchor;
        }

        public void SwitchToBellyCamera(float transitionTime = -1f)
        {
            if (_bellyCamera == null)
            {
                Debug.LogError("[CameraService] Belly camera is not assigned!");
                return;
            }

            if (_bellyAnchor == null)
            {
                Debug.LogError("[CameraService] Belly camera anchor is not registered!");
                return;
            }

            // Snap to the anchor before activating so the blend always starts
            // toward the hero's current belly position.
            _bellyCamera.transform.SetPositionAndRotation(_bellyAnchor.position, _bellyAnchor.rotation);

            if (_activeView != CameraView.Belly)
            {
                _viewBeforeBelly = _activeView;
            }

            if (_isometricCamera != null)
            {
                _isometricCamera.Priority = ISOMETRIC_PRIORITY_INACTIVE;
            }
            if (_combatCamera != null)
            {
                _combatCamera.Priority = COMBAT_PRIORITY_INACTIVE;
            }
            _bellyCamera.Priority = BELLY_PRIORITY_ACTIVE;
            _activeView = CameraView.Belly;

            float duration = transitionTime >= 0 ? transitionTime : _config?.TransitionTime ?? 2f;
            StartBellyFollow(duration);

            Debug.Log($"[CameraService] Switching to belly camera (time: {duration}s)");
        }

        public void SwitchToPreviousCamera(float transitionTime = -1f)
        {
            if (_viewBeforeBelly == CameraView.Combat)
            {
                SwitchToCombatCamera(transitionTime);
            }
            else
            {
                SwitchToIsometricCamera(transitionTime);
            }
        }

        // The belly camera has no tracking targets of its own, so while the belly
        // view is active it is glued to the anchor every frame. The anchor is a
        // hero child, which keeps the framing correct while the character rotates
        // to face the camera; Cinemachine blends to a moving target natively.
        private void StartBellyFollow(float duration)
        {
            if (_rotationTransitionCoroutine != null)
            {
                StopCoroutine(_rotationTransitionCoroutine);
            }
            _rotationTransitionCoroutine = StartCoroutine(BellyFollow(duration));
        }

        private IEnumerator BellyFollow(float duration)
        {
            _isTransitioning = true;
            float elapsed = 0f;

            while (_activeView == CameraView.Belly && _bellyAnchor != null)
            {
                _bellyCamera.transform.SetPositionAndRotation(_bellyAnchor.position, _bellyAnchor.rotation);

                elapsed += Time.deltaTime;
                if (_isTransitioning && elapsed >= duration)
                {
                    _isTransitioning = false;
                }

                yield return null;
            }

            _isTransitioning = false;
            _rotationTransitionCoroutine = null;
        }

        private void ApplyCombatCameraSettings()
        {
            if (_combatCamera == null || _config == null)
                return;
            
            // Apply orthographic size (Cinemachine 3.x removed m_ prefix)
            _combatCamera.Lens.OrthographicSize = _config.OrthographicSize;
            
            // Apply damping if using Cinemachine Position Composer
            // Note: In Cinemachine 3.x, FramingTransposer is replaced by CinemachinePositionComposer
            var positionComposer = _combatCamera.GetComponent<CinemachinePositionComposer>();
            if (positionComposer != null)
            {
                positionComposer.Damping.x = _config.DampingX;
                positionComposer.Damping.y = _config.DampingY;
                positionComposer.Damping.z = _config.DampingZ;
            }
        }
        
        private IEnumerator TransitionRotation(CinemachineCamera camera, Vector3 targetRotation, float duration)
        {
            _isTransitioning = true;
            
            Vector3 startRotation = camera.transform.eulerAngles;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Use smooth step for easing
                t = t * t * (3f - 2f * t);
                
                // Interpolate rotation
                Vector3 newRotation = Vector3.Lerp(startRotation, targetRotation, t);
                camera.transform.eulerAngles = newRotation;
                
                yield return null;
            }
            
            // Ensure final rotation is exact
            camera.transform.eulerAngles = targetRotation;
            
            _isTransitioning = false;
            _rotationTransitionCoroutine = null;
            
            Debug.Log($"[CameraService] Camera rotation transition completed to {targetRotation}");
        }
    }
}

