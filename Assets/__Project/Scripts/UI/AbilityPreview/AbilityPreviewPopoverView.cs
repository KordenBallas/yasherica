using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.AbilityPreview
{
    /// <summary>
    /// The shared ability-preview popover (one mechanism, two surfaces — mutation cards and the
    /// arena draft): name + description always; the 3D stage row shows the hero demonstrating
    /// the cast when the rig can build one, otherwise it hides and the popover reads as a text
    /// tooltip. Screen-clamped placement; holds no logic beyond that.
    /// </summary>
    public class AbilityPreviewPopoverView : MonoBehaviour, IAbilityPreviewPopover
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private RawImage _stageImage;
        [Tooltip("Offset from the hovered icon's screen position")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 48f);

        [Inject] private IAbilityPreviewStage _stage;

        public void Show(AbilityPreviewData data, Vector2 screenPosition)
        {
            if (data == null)
            {
                return;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = data.Name;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = data.Description;
            }

            if (_stageImage != null)
            {
                if (_stage.TryShow(data, out var texture))
                {
                    _stageImage.texture = texture;
                    _stageImage.gameObject.SetActive(true);
                }
                else
                {
                    _stageImage.gameObject.SetActive(false);
                }
            }

            Place(screenPosition + _screenOffset);
            if (_root != null)
            {
                // The view lives on the always-active canvas root (so Zenject resolves it from
                // the prefab); visibility toggles the popover body below it.
                _root.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _stage.Hide();
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void Place(Vector2 screenPosition)
        {
            if (_root == null)
            {
                return;
            }

            // Screen-space-overlay: world position == screen position. Clamp by the popover's
            // own scaled extents so it never leaves the screen.
            var extents = _root.rect.size * _root.lossyScale * 0.5f;
            var clamped = new Vector2(
                Mathf.Clamp(screenPosition.x, extents.x, Screen.width - extents.x),
                Mathf.Clamp(screenPosition.y, extents.y, Screen.height - extents.y));
            _root.position = clamped;
        }
    }
}
