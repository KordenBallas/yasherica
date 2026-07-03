using Combat.Player;
using Combat.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat.Input
{
    /// <summary>
    /// Thin adapter: raycasts the mouse against the overhead plan icons and replays the
    /// hovered entry's ghost — a queued player ability or an enemy's committed intent
    /// (reading the enemy). Hover-exit stops the replay. State-compare only; the ghost
    /// logic lives in GhostPlaybackPresenter.
    /// </summary>
    public sealed class AbilityIconHoverController : MonoBehaviour
    {
        private const float RaycastDistance = 200f;

        private GhostPlaybackPresenter _ghostPresenter;
        private Camera _camera;
        private AbilityIconMarker _hovered;

        public void Initialize(GhostPlaybackPresenter ghostPresenter)
        {
            _ghostPresenter = ghostPresenter;
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_ghostPresenter == null || Mouse.current == null)
                return;

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            var marker = FindHoveredMarker();
            if (marker == _hovered)
                return;

            _hovered = marker;

            if (_hovered == null)
            {
                _ghostPresenter.Stop();
            }
            else if (_hovered.IsEnemyIntent)
            {
                _ghostPresenter.PlayForEnemyIntent(_hovered.UnitId);
            }
            else
            {
                _ghostPresenter.PlayForQueueIndex(_hovered.UnitId, _hovered.QueueIndex);
            }
        }

        private AbilityIconMarker FindHoveredMarker()
        {
            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Component filter instead of a dedicated layer: icon colliders are tiny
            // triggers above head height, so scanning all hits stays cheap.
            foreach (var hit in Physics.RaycastAll(ray, RaycastDistance))
            {
                var marker = hit.collider.GetComponent<AbilityIconMarker>();
                if (marker != null)
                    return marker;
            }

            return null;
        }
    }
}
