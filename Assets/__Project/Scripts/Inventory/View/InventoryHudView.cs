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
        [Tooltip("Toggles feeding mode while the inventory is open")]
        [SerializeField] private Button _feedModeButton;
        [Tooltip("Optional highlight shown while feeding mode is active")]
        [SerializeField] private GameObject _feedModeActiveIndicator;

        [Header("Input")]
        [Tooltip("UI Cancel action (Escape) closing the inventory while it is open")]
        [SerializeField] private InputActionReference _cancelAction;

        public event Action OnOpenClicked;
        public event Action OnCloseClicked;
        public event Action OnFeedModeToggled;

        private void Awake()
        {
            _openButton.onClick.AddListener(HandleOpenButton);
            _closeButton.onClick.AddListener(HandleCloseButton);

            if (_feedModeButton != null)
            {
                _feedModeButton.onClick.AddListener(HandleFeedModeButton);
            }
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

            if (_feedModeButton != null)
            {
                _feedModeButton.onClick.RemoveListener(HandleFeedModeButton);
            }
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

        public void SetFeedModeToggleVisible(bool visible)
        {
            if (_feedModeButton != null)
            {
                _feedModeButton.gameObject.SetActive(visible);
            }
        }

        public void SetFeedModeActive(bool active)
        {
            if (_feedModeActiveIndicator != null)
            {
                _feedModeActiveIndicator.SetActive(active);
            }
        }

        private void HandleOpenButton()
        {
            OnOpenClicked?.Invoke();
        }

        private void HandleFeedModeButton()
        {
            OnFeedModeToggled?.Invoke();
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
