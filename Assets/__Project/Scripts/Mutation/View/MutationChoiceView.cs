using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// Thin adapter for the stage-up mutation choice panel: instantiates one
    /// <see cref="MutationChoiceButton"/> per offered option under an anchor and forwards the picked
    /// index to the presenter. Toggles a panel root with the choice's visibility. Holds no logic.
    /// </summary>
    public class MutationChoiceView : MonoBehaviour, IMutationChoiceView
    {
        [Tooltip("Root toggled with the choice's visibility")]
        [SerializeField] private GameObject _panelRoot;
        [Tooltip("Parent transform the choice buttons are laid out under")]
        [SerializeField] private Transform _choiceAnchor;
        [SerializeField] private MutationChoiceButton _choiceButtonPrefab;

        private readonly List<MutationChoiceButton> _buttons = new List<MutationChoiceButton>();

        public event Action<int> OnChoiceSelected;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        public void ShowChoices(IReadOnlyList<MutationChoiceViewData> options)
        {
            ClearButtons();
            if (options == null || _choiceButtonPrefab == null || _choiceAnchor == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                var button = Instantiate(_choiceButtonPrefab, _choiceAnchor);
                button.Configure(i, options[i], HandleChoiceSelected);
                _buttons.Add(button);
            }
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }

            if (!visible)
            {
                ClearButtons();
            }
        }

        private void HandleChoiceSelected(int index)
        {
            OnChoiceSelected?.Invoke(index);
        }

        private void ClearButtons()
        {
            foreach (var button in _buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _buttons.Clear();
        }
    }
}
