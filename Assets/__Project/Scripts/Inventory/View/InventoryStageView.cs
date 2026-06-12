using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Adapter on the inventory stage root. The stage GameObjects stay alive so
    /// coroutines and particles keep their state; only the overlay camera toggles,
    /// which both hides the diorama and disables its PhysicsRaycaster.
    /// </summary>
    public class InventoryStageView : MonoBehaviour, IInventoryStageView
    {
        [Header("Scene References")]
        [Tooltip("The stage's overlay camera (URP Render Type = Overlay)")]
        [SerializeField] private UnityEngine.Camera _stageCamera;

        private void Awake()
        {
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
    }
}
