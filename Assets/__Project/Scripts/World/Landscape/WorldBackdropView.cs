using UnityEngine;

namespace World.Landscape
{
    /// <summary>
    /// Thin adapter keeping the backdrop rig anchored to the run: it tracks the hero on X/Z
    /// instantly and on Y with slow smoothing — tier climbs re-center the horizon while jump arcs
    /// average out — so under the hero-following camera the horizon stays screen-stable and reads
    /// as infinitely distant (parallax is deferred polish, per the brief). No logic beyond the
    /// follow — the rig itself is assembled by <see cref="WorldBackdropBuilder"/>.
    /// </summary>
    public class WorldBackdropView : MonoBehaviour
    {
        private const float HeightSmoothTime = 1.5f;

        private Transform _followTarget;
        private float _currentY;
        private float _heightVelocity;

        public void Initialize(Transform followTarget)
        {
            _followTarget = followTarget;
            _currentY = followTarget != null ? followTarget.position.y : 0f;
        }

        private void LateUpdate()
        {
            if (_followTarget == null)
            {
                return;
            }

            Vector3 target = _followTarget.position;
            _currentY = Mathf.SmoothDamp(_currentY, target.y, ref _heightVelocity, HeightSmoothTime);
            transform.position = new Vector3(target.x, _currentY, target.z);
        }
    }
}
