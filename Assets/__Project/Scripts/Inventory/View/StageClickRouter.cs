using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Inventory.View
{
    /// <summary>
    /// Routes pointer clicks to bubbles on the inventory stage. The stage is drawn
    /// by a URP overlay camera, where the UGUI PhysicsRaycaster proved unreliable,
    /// so this adapter raycasts explicitly from the stage camera instead and
    /// forwards hits to the clicked <see cref="BubbleView"/>.
    /// </summary>
    public class StageClickRouter : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The stage overlay camera; routing is active only while it renders")]
        [SerializeField] private UnityEngine.Camera _stageCamera;

        [Header("Raycast")]
        [Tooltip("Layers that receive stage clicks (the InventoryFocus layer)")]
        [SerializeField] private LayerMask _clickMask;
        [SerializeField] private float _maxRayDistance = 50f;

        private void Update()
        {
            // A disabled overlay camera means the inventory is closed.
            if (_stageCamera == null || !_stageCamera.enabled)
            {
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            // HUD buttons (e.g. Close) must win over the 3D stage behind them.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryClickBubble(pointer.position.ReadValue());
        }

        private void TryClickBubble(Vector2 screenPosition)
        {
            var ray = _stageCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _clickMask))
            {
                return;
            }

            var bubble = hit.collider.GetComponentInParent<BubbleView>();
            if (bubble != null)
            {
                bubble.NotifyClicked();
            }
        }
    }
}
