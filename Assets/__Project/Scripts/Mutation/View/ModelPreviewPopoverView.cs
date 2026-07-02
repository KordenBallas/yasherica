using UnityEngine;
using UnityEngine.UI;

namespace Mutation.View
{
    /// <summary>
    /// The mutation card's mini-model popover: shows the preview rig's RenderTexture in a
    /// RawImage near the hovered part picture, clamped to the screen. Holds no logic.
    /// </summary>
    public class ModelPreviewPopoverView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RawImage _image;
        [Tooltip("Offset from the hovered part picture's screen position")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(220f, 0f);

        public void Show(Texture texture, Vector2 screenPosition)
        {
            if (_image != null)
            {
                _image.texture = texture;
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
            // popover's own scaled extents so it never leaves the screen.
            var extents = _root.rect.size * _root.lossyScale * 0.5f;
            var clamped = new Vector2(
                Mathf.Clamp(screenPosition.x, extents.x, Screen.width - extents.x),
                Mathf.Clamp(screenPosition.y, extents.y, Screen.height - extents.y));
            _root.position = clamped;
        }
    }
}
