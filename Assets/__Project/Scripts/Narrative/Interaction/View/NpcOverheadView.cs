using Narrative.Interaction.Core;
using TMPro;
using UnityEngine;

namespace Narrative.Interaction.View
{
    /// <summary>
    /// World-space overhead UI for one NPC: an always-visible name and intent marker plus an in-range F
    /// prompt, built from 3D <see cref="TextMeshPro"/> labels and billboarded to the camera each frame.
    /// A thin view — it renders what the presenter sets and never decides intent or proximity itself.
    /// </summary>
    public sealed class NpcOverheadView : MonoBehaviour, INpcOverheadView
    {
        private const float NameFontSize = 2.4f;
        private const float MarkerFontSize = 4f;
        private const float PromptFontSize = 2f;
        private const string PromptLabel = "[F] Talk";

        private static readonly Color QuestColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color HostileColor = new Color(1f, 0.3f, 0.25f);

        private TextMeshPro _nameLabel;
        private TextMeshPro _markerLabel;
        private TextMeshPro _promptLabel;
        private Camera _camera;

        private void Awake()
        {
            _nameLabel = CreateLabel("Name", new Vector3(0f, 0f, 0f), NameFontSize, Color.white);
            _markerLabel = CreateLabel("Marker", new Vector3(0f, 0.7f, 0f), MarkerFontSize, Color.white);
            _promptLabel = CreateLabel("Prompt", new Vector3(0f, -0.6f, 0f), PromptFontSize, Color.white);

            _markerLabel.gameObject.SetActive(false);
            _promptLabel.text = PromptLabel;
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

            // Billboard: face the camera so name/marker/prompt read from across the screen.
            transform.rotation = _camera.transform.rotation;
        }

        public void SetName(string displayName)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = displayName ?? string.Empty;
            }
        }

        public void SetIntentMarker(NpcIntent intent)
        {
            if (_markerLabel == null)
            {
                return;
            }

            switch (intent)
            {
                case NpcIntent.QuestBearer:
                    _markerLabel.text = "?";
                    _markerLabel.color = QuestColor;
                    _markerLabel.gameObject.SetActive(true);
                    break;
                case NpcIntent.Hostile:
                    _markerLabel.text = "!";
                    _markerLabel.color = HostileColor;
                    _markerLabel.gameObject.SetActive(true);
                    break;
                default:
                    _markerLabel.gameObject.SetActive(false);
                    break;
            }
        }

        public void ShowPrompt(bool visible)
        {
            if (_promptLabel != null)
            {
                _promptLabel.gameObject.SetActive(visible);
            }
        }

        public void DestroyView()
        {
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private TextMeshPro CreateLabel(string label, Vector3 localOffset, float fontSize, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
            return tmp;
        }
    }
}
