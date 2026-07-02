using TMPro;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// The mutation panel's ability tooltip: a small name + description box positioned near
    /// the hovered ability icon, clamped to the screen. Deliberately mutation-local — a
    /// shared UI tooltip service is a ROADMAP backlog item. Holds no logic beyond placement.
    /// </summary>
    public class AbilityTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [Tooltip("Offset from the hovered icon's screen position")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 48f);

        public void Show(string abilityName, string description, Vector2 screenPosition)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = abilityName;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = description;
            }

            Place(screenPosition + _screenOffset);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Place(Vector2 screenPosition)
        {
            if (_root == null)
            {
                return;
            }

            // Screen-space-overlay: world position == screen position. Clamp by the
            // tooltip's own scaled extents so it never leaves the screen.
            var extents = _root.rect.size * _root.lossyScale * 0.5f;
            var clamped = new Vector2(
                Mathf.Clamp(screenPosition.x, extents.x, Screen.width - extents.x),
                Mathf.Clamp(screenPosition.y, extents.y, Screen.height - extents.y));
            _root.position = clamped;
        }
    }
}
