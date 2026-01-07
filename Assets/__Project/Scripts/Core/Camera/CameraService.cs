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
        
        [Header("Configuration")]
        [SerializeField] private CameraConfig _config;
        
        private const int ISOMETRIC_PRIORITY_ACTIVE = 10;
        private const int ISOMETRIC_PRIORITY_INACTIVE = 0;
        private const int COMBAT_PRIORITY_ACTIVE = 20;
        private const int COMBAT_PRIORITY_INACTIVE = 0;
        
        private bool _isTransitioning;
        private Coroutine _rotationTransitionCoroutine;
        
        public bool IsTransitioning => _isTransitioning;
        
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
            _combatCamera.Priority = COMBAT_PRIORITY_ACTIVE;
            
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
            _isometricCamera.Priority = ISOMETRIC_PRIORITY_ACTIVE;
            
            // Start smooth rotation transition
            if (_rotationTransitionCoroutine != null)
            {
                StopCoroutine(_rotationTransitionCoroutine);
            }
            _rotationTransitionCoroutine = StartCoroutine(TransitionRotation(_isometricCamera, targetRotation, duration));
            
            Debug.Log($"[CameraService] Switching to isometric camera (rotation: {targetRotation}, time: {duration}s)");
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

