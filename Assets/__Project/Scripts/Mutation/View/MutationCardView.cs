using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mutation.View
{
    /// <summary>
    /// Thin adapter for a single mutation card: renders the offered part (front face: picture +
    /// ability icons), the part it would replace (back face, reached via the corner flip button;
    /// "nothing replaced" for an empty slot), the belonging tint, and the placeholder potency
    /// glow (frame brightness scales with rarity tier). Forwards clicks and hovers to
    /// <see cref="MutationChoiceView"/>, which owns selection state and the tooltip/popover;
    /// the card holds presentation state only (its flipped face).
    /// </summary>
    public class MutationCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [Tooltip("Frame graphic carrying the belonging tint x tier-glow brightness")]
        [SerializeField] private Graphic _frameGlow;

        [Header("Front face (the offered part)")]
        [SerializeField] private GameObject _frontRoot;
        [SerializeField] private TextMeshProUGUI _partName;
        [Tooltip("The pictured part; hovering it opens the mini-model popover")]
        [SerializeField] private Image _partImage;
        [SerializeField] private PointerHoverRelay _partImageHover;
        [SerializeField] private Transform _abilityRow;

        [Header("Back face (the replaced part)")]
        [SerializeField] private GameObject _backRoot;
        [SerializeField] private TextMeshProUGUI _backPartName;
        [SerializeField] private Image _backPartImage;
        [SerializeField] private Transform _backAbilityRow;
        [Tooltip("Shown instead of the back part visuals when the slot is empty")]
        [SerializeField] private GameObject _nothingReplacedLabel;

        [Header("Controls")]
        [SerializeField] private Button _flipButton;
        [SerializeField] private GameObject _selectedHighlight;
        [Tooltip("The 'choose again to graft' confirm affordance, shown while selected")]
        [SerializeField] private GameObject _confirmHint;
        [SerializeField] private MutationAbilityIconView _abilityIconPrefab;

        [Header("Tier glow (placeholder: brightness by rarity tier)")]
        [SerializeField] private float _glowBaseIntensity = 0.6f;
        [SerializeField] private float _glowPerTier = 0.15f;

        private int _index;
        private bool _showingBack;
        private Action<int> _onClicked;
        private Action<int, Vector2, bool> _onPartHover;
        private Action<MutationAbilityIconViewData, Vector2, bool> _onAbilityHover;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }

            if (_flipButton != null)
            {
                _flipButton.onClick.AddListener(HandleFlipClicked);
            }

            if (_partImageHover != null)
            {
                _partImageHover.Entered += HandlePartHoverEntered;
                _partImageHover.Exited += HandlePartHoverExited;
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }

            if (_flipButton != null)
            {
                _flipButton.onClick.RemoveListener(HandleFlipClicked);
            }

            if (_partImageHover != null)
            {
                _partImageHover.Entered -= HandlePartHoverEntered;
                _partImageHover.Exited -= HandlePartHoverExited;
            }
        }

        public void Configure(
            int index,
            MutationChoiceViewData data,
            Action<int> onClicked,
            Action<int, Vector2, bool> onPartHover,
            Action<MutationAbilityIconViewData, Vector2, bool> onAbilityHover)
        {
            _index = index;
            _onClicked = onClicked;
            _onPartHover = onPartHover;
            _onAbilityHover = onAbilityHover;

            ConfigureFace(data.Front, _partName, _partImage, _abilityRow);

            if (data.HasReplacedPart)
            {
                ConfigureFace(data.Back, _backPartName, _backPartImage, _backAbilityRow);
            }

            SetBackFaceMode(hasReplacedPart: data.HasReplacedPart);
            ApplyFrameGlow(data.Tint, data.RarityTier);
            ShowFace(showBack: false);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetActive(selected);
            }

            if (_confirmHint != null)
            {
                _confirmHint.SetActive(selected);
            }
        }

        private void ConfigureFace(
            MutationCardFaceViewData face, TextMeshProUGUI nameLabel, Image image, Transform abilityRow)
        {
            if (nameLabel != null)
            {
                nameLabel.text = face.PartName;
            }

            if (image != null)
            {
                image.sprite = face.PartIcon;
                image.enabled = face.PartIcon != null;
            }

            if (abilityRow == null || _abilityIconPrefab == null)
            {
                return;
            }

            foreach (var ability in face.Abilities)
            {
                // Icons live under the face root, so they are destroyed with the card.
                var icon = Instantiate(_abilityIconPrefab, abilityRow);
                icon.Configure(ability, _onAbilityHover);
            }
        }

        // An empty slot renders the bare-slot back ("nothing replaced"), not a false comparison.
        private void SetBackFaceMode(bool hasReplacedPart)
        {
            if (_backPartName != null)
            {
                _backPartName.gameObject.SetActive(hasReplacedPart);
            }

            if (_backPartImage != null)
            {
                _backPartImage.gameObject.SetActive(hasReplacedPart);
            }

            if (_nothingReplacedLabel != null)
            {
                _nothingReplacedLabel.SetActive(!hasReplacedPart);
            }
        }

        // Placeholder tier-glow grammar: colour = belonging, brightness = potency tier.
        private void ApplyFrameGlow(Color tint, int rarityTier)
        {
            if (_frameGlow == null)
            {
                return;
            }

            var intensity = _glowBaseIntensity + _glowPerTier * rarityTier;
            var glow = tint * intensity;
            glow.a = tint.a;
            _frameGlow.color = glow;
        }

        private void ShowFace(bool showBack)
        {
            _showingBack = showBack;
            if (_frontRoot != null)
            {
                _frontRoot.SetActive(!showBack);
            }

            if (_backRoot != null)
            {
                _backRoot.SetActive(showBack);
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }

        private void HandleFlipClicked()
        {
            ShowFace(!_showingBack);
        }

        private void HandlePartHoverEntered()
        {
            _onPartHover?.Invoke(_index, PartImageScreenPosition(), true);
        }

        private void HandlePartHoverExited()
        {
            _onPartHover?.Invoke(_index, PartImageScreenPosition(), false);
        }

        // On a screen-space-overlay canvas a RectTransform's world position IS its screen position.
        private Vector2 PartImageScreenPosition()
        {
            return _partImage != null ? (Vector2)_partImage.transform.position : Vector2.zero;
        }
    }
}
