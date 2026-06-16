using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mutation.View
{
    /// <summary>
    /// Thin adapter for a single mutation-choice button: renders one option (label, icon, archetype
    /// tint) and forwards its click as the option's index. Instantiated by <see cref="MutationChoiceView"/>;
    /// holds no choice logic.
    /// </summary>
    public class MutationChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [Tooltip("Optional icon image; hidden when the option has no icon")]
        [SerializeField] private Image _icon;
        [Tooltip("Optional graphic tinted with the archetype accent colour")]
        [SerializeField] private Graphic _tintTarget;

        private int _index;
        private Action<int> _onClicked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Configure(int index, MutationChoiceViewData data, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            if (_label != null)
            {
                _label.text = data.DisplayName;
            }

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
            }

            if (_tintTarget != null)
            {
                _tintTarget.color = data.Tint;
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }
    }
}
