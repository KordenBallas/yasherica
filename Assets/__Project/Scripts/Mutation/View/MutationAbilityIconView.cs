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
        [SerializeField] private PointerHoverRelay _hoverRelay;

        private MutationAbilityIconViewData _data;
        private Action<MutationAbilityIconViewData, Vector2, bool> _onHover;

        private void Awake()
        {
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
