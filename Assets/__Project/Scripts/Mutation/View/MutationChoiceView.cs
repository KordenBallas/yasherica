using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Mutation.View
{
    /// <summary>
    /// Thin adapter for the mutation card hand: instantiates one <see cref="MutationCardView"/>
    /// per offered variant under an anchor, owns the presentation-only interaction state —
    /// two-step selection (first click selects a card, a second click on the same card
    /// confirms and raises <see cref="OnChoiceSelected"/>), the shared ability-preview
    /// popover (hover an ability icon → description + the hero casting it), and the
    /// mini-model popover (via <see cref="IMutationModelPreview"/>) — and toggles a panel
    /// root with the choice's visibility. No business logic: what the cards show and what a
    /// confirmed pick does is the presenter's.
    /// </summary>
    public class MutationChoiceView : MonoBehaviour, IMutationChoiceView
    {
        private const int NoSelection = -1;

        [Tooltip("Root toggled with the choice's visibility")]
        [SerializeField] private GameObject _panelRoot;
        [Tooltip("Parent transform the cards are laid out under")]
        [SerializeField] private Transform _choiceAnchor;
        [SerializeField] private MutationCardView _cardPrefab;
        [SerializeField] private ModelPreviewPopoverView _modelPopover;

        [Inject] private IMutationModelPreview _modelPreview;
        [Inject] private UI.AbilityPreview.IAbilityPreviewPopover _abilityPopover;

        private readonly List<MutationCardView> _cards = new List<MutationCardView>();
        private readonly List<MutationChoiceViewData> _shown = new List<MutationChoiceViewData>();
        private int _selectedIndex = NoSelection;

        public event Action<int> OnChoiceSelected;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            HideOverlays();
        }

        public void ShowChoices(IReadOnlyList<MutationChoiceViewData> options)
        {
            ClearCards();
            if (options == null || _cardPrefab == null || _choiceAnchor == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                var card = Instantiate(_cardPrefab, _choiceAnchor);
                card.Configure(i, options[i], HandleCardClicked, HandlePartHover, HandleAbilityHover);
                _cards.Add(card);
                _shown.Add(options[i]);
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
                ClearCards();
            }
        }

        // Two-step commit: the first click selects (weighty confirmation, not an idle tap);
        // a second click on the already-selected card confirms. Clicking another card
        // re-selects, never confirms.
        private void HandleCardClicked(int index)
        {
            if (index == _selectedIndex)
            {
                OnChoiceSelected?.Invoke(index);
                return;
            }

            SelectCard(index);
        }

        private void SelectCard(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _cards.Count && _cards[_selectedIndex] != null)
            {
                _cards[_selectedIndex].SetSelected(false);
            }

            _selectedIndex = index;
            if (index >= 0 && index < _cards.Count && _cards[index] != null)
            {
                _cards[index].SetSelected(true);
            }
        }

        private void HandlePartHover(int index, Vector2 screenPosition, bool entering)
        {
            if (!entering)
            {
                _modelPreview?.Hide();
                if (_modelPopover != null)
                {
                    _modelPopover.Hide();
                }

                return;
            }

            if (index < 0 || index >= _shown.Count || _modelPopover == null || _modelPreview == null)
            {
                return;
            }

            var data = _shown[index];
            if (_modelPreview.TryShow(data.SlotId, data.PartId, out var texture))
            {
                _modelPopover.Show(texture, screenPosition);
            }
        }

        private void HandleAbilityHover(
            MutationAbilityIconViewData ability, Vector2 screenPosition, bool entering)
        {
            if (entering)
            {
                _abilityPopover.Show(new UI.AbilityPreview.AbilityPreviewData(
                        ability.Name, ability.Description, ability.IsPassive,
                        ability.IsLine, ability.LineLength, ability.RingRadius, ability.AnimationTrigger),
                    screenPosition);
            }
            else
            {
                _abilityPopover.Hide();
            }
        }

        private void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            _shown.Clear();
            _selectedIndex = NoSelection;
            HideOverlays();
        }

        private void HideOverlays()
        {
            _abilityPopover?.Hide();

            if (_modelPopover != null)
            {
                _modelPopover.Hide();
            }

            _modelPreview?.Hide();
        }
    }
}
