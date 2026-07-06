using TMPro;
using UnityEngine;

namespace Hub.View
{
    /// <summary>
    /// World-space overhead labels for one Hub spot (O1 rework): an always-visible name plus an
    /// in-range F prompt with settable text, built from 3D <see cref="TextMeshPro"/> labels and
    /// billboarded to the camera each frame — the <c>NpcOverheadView</c> treatment with a
    /// per-spot prompt. A thin view — it renders what the presenter sets.
    /// </summary>
    public sealed class HubOverheadLabelView : MonoBehaviour, IHubPromptView
    {
        private const float NameFontSize = 2.4f;
        private const float PromptFontSize = 2f;

        private TextMeshPro _nameLabel;
        private TextMeshPro _promptLabel;
        private Camera _camera;

        private void Awake()
        {
            _nameLabel = CreateLabel("Name", new Vector3(0f, 0f, 0f), NameFontSize);
            _promptLabel = CreateLabel("Prompt", new Vector3(0f, -0.6f, 0f), PromptFontSize);
            _promptLabel.gameObject.SetActive(false);
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            // Billboard: face the camera so the name and prompt read from across the screen.
            transform.rotation = _camera.transform.rotation;
        }

        public void SetName(string displayName)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = displayName ?? string.Empty;
            }
        }

        public void SetPrompt(string prompt)
        {
            if (_promptLabel != null)
            {
                _promptLabel.text = prompt ?? string.Empty;
            }
        }

        public void ShowPrompt(bool visible)
        {
            if (_promptLabel != null)
            {
                _promptLabel.gameObject.SetActive(visible);
            }
        }

        private TextMeshPro CreateLabel(string label, Vector3 localOffset, float fontSize)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);
            return tmp;
        }
    }
}
