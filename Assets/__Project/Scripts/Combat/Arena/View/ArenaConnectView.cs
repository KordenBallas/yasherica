using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.Arena.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the connect panel: two mode buttons + the address field +
    /// the host's Start button and a status line. No logic — flow lives in the presenter.
    /// </summary>
    public class ArenaConnectView : MonoBehaviour, IArenaConnectView
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TMP_InputField _addressInput;
        [SerializeField] private Button _startButton;
        [SerializeField] private TMP_Text _statusText;

        public event Action HostClicked;
        public event Action<string> JoinClicked;
        public event Action StartClicked;

        private void Awake()
        {
            _hostButton.onClick.AddListener(HandleHostClicked);
            _joinButton.onClick.AddListener(HandleJoinClicked);
            _startButton.onClick.AddListener(HandleStartClicked);
        }

        private void OnDestroy()
        {
            _hostButton.onClick.RemoveListener(HandleHostClicked);
            _joinButton.onClick.RemoveListener(HandleJoinClicked);
            _startButton.onClick.RemoveListener(HandleStartClicked);
        }

        public void SetStatus(string message)
        {
            _statusText.text = message;
        }

        public void SetConnectControlsInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _joinButton.interactable = interactable;
            _addressInput.interactable = interactable;
        }

        public void SetStartButtonVisible(bool visible)
        {
            _startButton.gameObject.SetActive(visible);
        }

        public void SetStartButtonInteractable(bool interactable)
        {
            _startButton.interactable = interactable;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleHostClicked() => HostClicked?.Invoke();

        private void HandleJoinClicked() => JoinClicked?.Invoke(_addressInput.text);

        private void HandleStartClicked() => StartClicked?.Invoke();
    }
}
