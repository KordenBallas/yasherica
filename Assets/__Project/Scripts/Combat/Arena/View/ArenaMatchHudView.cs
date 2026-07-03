using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.Arena.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the in-match HUD: status line, spectating label, desync
    /// warning, and the Leave button. No logic — flow lives in <see cref="Combat.Arena.ArenaMatchHudPresenter"/>.
    /// </summary>
    public class ArenaMatchHudView : MonoBehaviour, IArenaMatchHudView
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _spectateText;
        [SerializeField] private TMP_Text _desyncText;
        [SerializeField] private Button _leaveButton;

        public event Action LeaveClicked;

        private void Awake()
        {
            _leaveButton.onClick.AddListener(HandleLeaveClicked);
        }

        private void OnDestroy()
        {
            _leaveButton.onClick.RemoveListener(HandleLeaveClicked);
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        public void SetSpectatingVisible(bool visible)
        {
            if (_spectateText != null)
            {
                _spectateText.gameObject.SetActive(visible);
            }
        }

        public void SetLeaveVisible(bool visible)
        {
            _leaveButton.gameObject.SetActive(visible);
        }

        public void ShowDesyncWarning()
        {
            if (_desyncText != null)
            {
                _desyncText.gameObject.SetActive(true);
            }
        }

        private void HandleLeaveClicked() => LeaveClicked?.Invoke();
    }
}
