using Core.Logging;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// Adapter on the inventory stage root. The stage GameObjects stay alive so
    /// coroutines and particles keep their state; only the overlay camera toggles,
    /// which both hides the diorama and disables its raycast routing.
    /// </summary>
    public class InventoryStageView : MonoBehaviour, IInventoryStageView
    {
        [Header("Scene References")]
        [Tooltip("The stage's overlay camera (URP Render Type = Overlay)")]
        [SerializeField] private UnityEngine.Camera _stageCamera;

        [Inject] private IGameLogger _logger;

        private void Awake()
        {
            // The stage starts hidden; the presenter activates it on open.
            SetStageActive(false);
        }

        public void SetStageActive(bool active)
        {
            if (_stageCamera == null)
            {
                _logger?.Error(LogCategory.Inventory, "[InventoryStageView] Stage camera is not assigned!");
                return;
            }

            _stageCamera.enabled = active;
        }
    }
}
