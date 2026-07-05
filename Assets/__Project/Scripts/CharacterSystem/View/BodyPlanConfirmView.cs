using System;
using System.Text;
using CharacterSystem.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterSystem.View
{
    /// <summary>
    /// Thin uGUI adapter for the body-plan confirm modal: fills the target-frame title and
    /// the shed-part list, forwards the two button clicks. No business logic — the presenter
    /// decides when to show and what a decision means.
    /// </summary>
    public class BodyPlanConfirmView : MonoBehaviour, IBodyPlanConfirmView
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _shedListLabel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _declineButton;

        private readonly StringBuilder _builder = new StringBuilder();

        public event Action OnConfirmed;
        public event Action OnDeclined;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
            _declineButton.onClick.AddListener(HandleDeclineClicked);
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            _declineButton.onClick.RemoveListener(HandleDeclineClicked);
        }

        public void Show(BodyPlanChangeSummary summary)
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = $"Your body will re-form into: {summary.TargetFrameName}";
            }

            if (_shedListLabel != null)
            {
                _builder.Clear();
                _builder.Append("These parts have nowhere to attach and will return to your inventory:");
                foreach (var name in summary.ShedPartNames)
                {
                    _builder.Append("\n• ").Append(name);
                }

                _shedListLabel.text = _builder.ToString();
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void HandleConfirmClicked()
        {
            OnConfirmed?.Invoke();
        }

        private void HandleDeclineClicked()
        {
            OnDeclined?.Invoke();
        }
    }
}
