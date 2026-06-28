using DevTools.Core;
using UnityEngine;
using Zenject;

namespace DevTools.View
{
    /// <summary>
    /// Thin IMGUI renderer for the developer state overlay: draws the sections the injected
    /// <see cref="IDevStateSource"/> builds, toggled with a key (default F1). No game logic — it only
    /// queries the source and renders. Created by <c>DevToolsInstaller</c> on a fresh GameObject; only
    /// installed in the editor / development builds.
    /// </summary>
    public sealed class DevOverlayView : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const float PanelWidth = 460f;

        private IDevStateSource _source;
        private bool _visible = true;
        private Vector2 _scroll;
        private GUIStyle _titleStyle;

        [Inject]
        public void Construct(IDevStateSource source)
        {
            _source = source;
        }

        private void OnGUI()
        {
            if (_source == null)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == ToggleKey)
            {
                _visible = !_visible;
                Event.current.Use();
            }

            if (!_visible)
            {
                return;
            }

            EnsureStyles();

            GUILayout.BeginArea(new Rect(8f, 8f, PanelWidth, Screen.height - 16f), GUI.skin.box);
            GUILayout.Label($"Dev State  (F1 to hide)", _titleStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var section in _source.BuildSections())
            {
                GUILayout.Space(6f);
                GUILayout.Label(section.Title, _titleStyle);
                for (int i = 0; i < section.Rows.Count; i++)
                {
                    GUILayout.Label(section.Rows[i]);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        }
    }
}
