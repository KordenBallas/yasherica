using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mutation.View
{
    /// <summary>
    /// Thin adapter for one ability icon on a mutation card face: shows the ability's icon
    /// (with a marker distinguishing passives) and relays hover enter/exit — with the icon's
    /// screen position — so the panel can drive the shared tooltip. Holds no logic.
    /// </summary>
    public class MutationAbilityIconView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [Tooltip("Marker shown on passive (always-on) abilities")]
        [SerializeField] private GameObject _passiveMarker;
        [Tooltip("Effect badge: shows the applied status's glyph (FR8); its prefab-authored " +
                 "sprite is the neutral untyped mark used when the ability applies no status")]
        [SerializeField] private Image _statusBadge;
        [SerializeField] private PointerHoverRelay _hoverRelay;

        private MutationAbilityIconViewData _data;
        private Action<MutationAbilityIconViewData, Vector2, bool> _onHover;
        private Sprite _untypedBadgeSprite;

        private void Awake()
        {
            if (_statusBadge != null)
            {
                _untypedBadgeSprite = _statusBadge.sprite;
            }

            if (_hoverRelay != null)
            {
                _hoverRelay.Entered += HandleHoverEntered;
                _hoverRelay.Exited += HandleHoverExited;
            }
        }

        private void OnDestroy()
        {
            if (_hoverRelay != null)
            {
                _hoverRelay.Entered -= HandleHoverEntered;
                _hoverRelay.Exited -= HandleHoverExited;
            }
        }

        public void Configure(
            MutationAbilityIconViewData data,
            Action<MutationAbilityIconViewData, Vector2, bool> onHover)
        {
            _data = data;
            _onHover = onHover;

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
            }

            if (_passiveMarker != null)
            {
                _passiveMarker.SetActive(data.IsPassive);
            }

            if (_statusBadge != null)
            {
                _statusBadge.sprite = data.StatusGlyph != null ? data.StatusGlyph : _untypedBadgeSprite;
                _statusBadge.enabled = _statusBadge.sprite != null;
            }
        }

        private void HandleHoverEntered()
        {
            _onHover?.Invoke(_data, ScreenPosition(), true);
        }

        private void HandleHoverExited()
        {
            _onHover?.Invoke(_data, ScreenPosition(), false);
        }

        // On a screen-space-overlay canvas a RectTransform's world position IS its screen position.
        private Vector2 ScreenPosition()
        {
            return transform.position;
        }
    }
}
