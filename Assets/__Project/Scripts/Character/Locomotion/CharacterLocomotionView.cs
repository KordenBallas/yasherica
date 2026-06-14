using CharacterSystem.Runtime;
using UnityEngine;

namespace Character.Locomotion
{
    /// <summary>
    /// Thin MonoBehaviour adapter on the Hero root implementing <see cref="ILocomotionView"/>:
    /// writes the run blend parameter onto the assembled rig's Animator and yaws the host
    /// root to a heading. No locomotion logic lives here — the presenter computes everything.
    /// </summary>
    public class CharacterLocomotionView : MonoBehaviour, ILocomotionView
    {
        [Tooltip("Bridge that assembles the rig at runtime; supplies the Animator the run blend drives.")]
        [SerializeField] private ModularCharacterVisual _characterVisual;

        private Animator _animator;
        private bool _hasSpeedParameter;

        public void SetMotionSpeed(float normalizedSpeed)
        {
            if (!TryGetAnimator(out var animator))
            {
                return;
            }

            // Only write Speed if the active controller actually exposes it (e.g. before the run
            // blend tree is applied the rig may carry an idle-only controller). Re-check while false
            // so a late-initialized animator is still picked up; stop once found.
            if (!_hasSpeedParameter)
            {
                _hasSpeedParameter = HasFloatParameter(animator, LocomotionAnimatorParameters.Speed);
            }

            if (_hasSpeedParameter)
            {
                animator.SetFloat(LocomotionAnimatorParameters.Speed, normalizedSpeed);
            }
        }

        private static bool HasFloatParameter(Animator animator, string parameterName)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetFacingYaw(float yawDegrees)
        {
            // Yaw-only: movement is planar, and rotating the host root carries the parented rig.
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        // The rig (and its Animator) is assembled in ModularCharacterVisual.Start, so the
        // reference is resolved lazily and cached the first frame it becomes available.
        private bool TryGetAnimator(out Animator animator)
        {
            if (_animator == null && _characterVisual != null)
            {
                _animator = _characterVisual.Animator;
            }

            animator = _animator;
            return animator != null;
        }
    }
}
