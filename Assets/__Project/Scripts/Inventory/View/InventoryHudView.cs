using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inventory.View
{
    /// <summary>
    /// Thin uGUI adapter for the inventory HUD: open icon, close button and the
    /// cancel input action (Escape). Forwards interactions to the presenter.
    /// </summary>
    public class InventoryHudView : MonoBehaviour, IInventoryHudView
    {
        [Header("Buttons")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;

        [Header("Input")]
        [Tooltip("UI Cancel action (Escape) closing the inventory while it is open")]
        [SerializeField] private InputActionReference _cancelAction;

        public event Action OnOpenClicked;
        public event Action OnCloseClicked;

        private void Awake()
        {
            _openButton.onClick.AddListener(HandleOpenButton);
            _closeButton.onClick.AddListener(HandleCloseButton);
        }

        private void OnEnable()
        {
            if (_cancelAction?.action != null)
            {
                _cancelAction.action.Enable();
                _cancelAction.action.performed += HandleCancelPerformed;
            }
        }

        private void OnDisable()
        {
            if (_cancelAction?.action != null)
            {
                _cancelAction.action.performed -= HandleCancelPerformed;
            }
        }

        private void OnDestroy()
        {
            _openButton.onClick.RemoveListener(HandleOpenButton);
            _closeButton.onClick.RemoveListener(HandleCloseButton);
        }

        public void SetOpenButtonVisible(bool visible)
        {
            _openButton.gameObject.SetActive(visible);
        }

        public void SetOpenButtonInteractable(bool interactable)
        {
            _openButton.interactable = interactable;
        }

        public void SetCloseButtonVisible(bool visible)
        {
            _closeButton.gameObject.SetActive(visible);
        }

        private void HandleOpenButton()
        {
            OnOpenClicked?.Invoke();
        }

        private void HandleCloseButton()
        {
            OnCloseClicked?.Invoke();
        }

        private void HandleCancelPerformed(InputAction.CallbackContext _)
        {
            // Cancel only acts as "close": the presenter ignores it when already closed.
            OnCloseClicked?.Invoke();
        }
    }
}
