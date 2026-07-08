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

        [Tooltip("Seat-liveness line (X1); when left empty a placeholder is cloned off the status line.")]
        [SerializeField] private TMP_Text _seatNoticeText;

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

        public void SetSeatNotice(string message)
        {
            if (_seatNoticeText == null)
            {
                if (string.IsNullOrEmpty(message) || _statusText == null)
                    return;

                // Code-built placeholder (the D2 turn-strip precedent): clone the status line so
                // the font/canvas setup rides along, sit one line below it.
                _seatNoticeText = Instantiate(_statusText, _statusText.transform.parent);
                _seatNoticeText.name = "SeatNoticeText";
                _seatNoticeText.rectTransform.anchoredPosition =
                    _statusText.rectTransform.anchoredPosition + Vector2.down * _statusText.fontSize * 1.4f;
            }

            bool visible = !string.IsNullOrEmpty(message);
            _seatNoticeText.gameObject.SetActive(visible);
            _seatNoticeText.text = visible ? message : string.Empty;
        }

        private void HandleLeaveClicked() => LeaveClicked?.Invoke();
    }
}
