using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Inventory.View
{
    /// <summary>
    /// Routes pointer input to the 3D inventory stage. The stage is drawn by a URP
    /// overlay camera, where the UGUI PhysicsRaycaster proved unreliable, so this
    /// adapter raycasts explicitly from the stage camera instead.
    ///
    /// Press-and-release below the drag threshold is a click, forwarded to the hit
    /// <see cref="IStageClickable"/> (pot bubbles, blank sockets). Pressing a pot
    /// bubble and moving past the threshold starts a drag: the bubble follows the
    /// pointer on its camera-distance plane (the pot's drift skips dragged bubbles);
    /// releasing over a blank socket drops the artifact into it, releasing anywhere
    /// else snaps the bubble back into the pot.
    /// </summary>
    public class StageDragRouter : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The stage overlay camera; routing is active only while it renders")]
        [SerializeField] private UnityEngine.Camera _stageCamera;

        [Header("Raycast")]
        [Tooltip("Layers that receive stage clicks (the InventoryFocus layer)")]
        [SerializeField] private LayerMask _clickMask;
        [SerializeField] private float _maxRayDistance = 50f;

        [Header("Drag")]
        [Tooltip("Pointer travel (pixels) before a press on a bubble becomes a drag instead of a click")]
        [SerializeField, Min(1f)] private float _dragThresholdPixels = 12f;

        private IStageClickable _pressedClickable;
        private BubbleView _pressedBubble;
        private Vector2 _pressScreenPosition;
        private float _dragPlaneDistance;
        private bool _dragging;

        private void Update()
        {
            // A disabled overlay camera means the inventory is closed.
            if (_stageCamera == null || !_stageCamera.enabled)
            {
                CancelDrag();
                ClearPress();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            var screenPosition = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                HandlePress(screenPosition);
            }
            else if (pointer.press.isPressed)
            {
                HandleHold(screenPosition);
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                HandleRelease(screenPosition);
            }
        }

        private void HandlePress(Vector2 screenPosition)
        {
            // HUD buttons (e.g. Close) must win over the 3D stage behind them.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!TryRaycast(screenPosition, out RaycastHit hit))
            {
                return;
            }

            _pressedClickable = hit.collider.GetComponentInParent<IStageClickable>();
            _pressedBubble = hit.collider.GetComponentInParent<BubbleView>();
            _pressScreenPosition = screenPosition;
            _dragPlaneDistance = hit.distance;
        }

        private void HandleHold(Vector2 screenPosition)
        {
            if (!_dragging
                && _pressedBubble != null
                && (screenPosition - _pressScreenPosition).sqrMagnitude
                   >= _dragThresholdPixels * _dragThresholdPixels)
            {
                // The dragged bubble leaves the drift and stops occluding the
                // release raycast (its collider is what the pointer hovers).
                _dragging = true;
                _pressedBubble.SetDragged(true);
                _pressedBubble.SetInteractable(false);
            }

            if (_dragging && _pressedBubble != null)
            {
                var ray = _stageCamera.ScreenPointToRay(screenPosition);
                _pressedBubble.transform.position = ray.GetPoint(_dragPlaneDistance);
            }
        }

        private void HandleRelease(Vector2 screenPosition)
        {
            if (_dragging)
            {
                if (_pressedBubble != null
                    && TryRaycast(screenPosition, out RaycastHit hit))
                {
                    var dropTarget = hit.collider.GetComponentInParent<IArtifactDropTarget>();
                    dropTarget?.NotifyArtifactDropped(_pressedBubble.InstanceId);
                }

                // A rejected/ignored drop simply snaps back: the drift re-takes the
                // bubble on its unchanged base point next frame.
                CancelDrag();
            }
            else if (_pressedClickable != null)
            {
                _pressedClickable.NotifyClicked();
            }

            ClearPress();
        }

        private void CancelDrag()
        {
            if (_dragging && _pressedBubble != null)
            {
                _pressedBubble.SetDragged(false);
                _pressedBubble.SetInteractable(true);
            }

            _dragging = false;
        }

        private void ClearPress()
        {
            _pressedClickable = null;
            _pressedBubble = null;
        }

        private bool TryRaycast(Vector2 screenPosition, out RaycastHit hit)
        {
            var ray = _stageCamera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, _maxRayDistance, _clickMask);
        }
    }
}
