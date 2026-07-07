using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameInput.View
{
    /// <summary>
    /// The touch control overlay (Input Foundation R3): an on-screen stick that feeds Move and an
    /// on-screen interact button, both driving a virtual gamepad the shared actions already listen to
    /// (the active-source classifier reports that virtual device as Touch, not Gamepad). The hierarchy
    /// is built procedurally — same precedent as <c>NpcOverheadView</c> — so no prefab with hand-rolled
    /// script GUIDs is needed. Shows only when a touchscreen is present; plain shapes on purpose, glyph
    /// art is a deferred brief.
    /// </summary>
    public sealed class TouchControlsView : MonoBehaviour
    {
        private const string MoveStickControlPath = "<Gamepad>/leftStick";
        private const string InteractButtonControlPath = "<Gamepad>/buttonNorth";

        private const float StickBackgroundSize = 260f;
        private const float StickKnobSize = 110f;
        private const float StickMovementRange = 75f;
        private const float InteractButtonSize = 170f;
        private static readonly Vector2 StickAnchoredPosition = new Vector2(230f, 230f);
        private static readonly Vector2 InteractAnchoredPosition = new Vector2(-230f, 230f);
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Color BackgroundTint = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color KnobTint = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color ButtonTint = new Color(1f, 1f, 1f, 0.30f);

        private void Awake()
        {
            if (Touchscreen.current == null)
            {
                gameObject.SetActive(false);
                return;
            }

            EnsureEventSystemExists();
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            gameObject.AddComponent<GraphicRaycaster>();

            BuildMoveStick();
            BuildInteractButton();
        }

        private void BuildMoveStick()
        {
            var background = CreateImage("MoveStickBackground", transform,
                new Vector2(0f, 0f), StickAnchoredPosition, StickBackgroundSize, BackgroundTint);

            var knob = CreateImage("MoveStickKnob", background.transform,
                new Vector2(0.5f, 0.5f), Vector2.zero, StickKnobSize, KnobTint);

            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = MoveStickControlPath;
            stick.movementRange = StickMovementRange;
        }

        private void BuildInteractButton()
        {
            var button = CreateImage("InteractButton", transform,
                new Vector2(1f, 0f), InteractAnchoredPosition, InteractButtonSize, ButtonTint);

            var onScreenButton = button.gameObject.AddComponent<OnScreenButton>();
            onScreenButton.controlPath = InteractButtonControlPath;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(button.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "TALK";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 36f;
            label.color = Color.white;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchor,
            Vector2 anchoredPosition, float size, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = tint;

            var rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(size, size);
            return image;
        }

        private void EnsureEventSystemExists()
        {
            // On-screen controls route through pointer events; without an EventSystem they are inert.
            // Same lazy-create pattern as DialogueView.
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
