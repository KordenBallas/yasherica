using Inventory.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor.Inventory
{
    /// <summary>
    /// One-click setup for the feeding-mode presentation, so the prefab objects do not have to be
    /// authored by hand. Builds, on the two inventory prefabs:
    ///  - InventoryStage: a FeedingTrayAnchor below the pot (3D bubbles) + a screen-space readout
    ///    canvas to the right of the pot (cumulative / progress / dominant labels, a digestion bar,
    ///    and the Feed button), with a wired <see cref="FeedingView"/>.
    ///  - InventoryHud: a feed-mode toggle button (+ active indicator) wired into
    ///    <see cref="InventoryHudView"/>.
    /// Idempotent: rerunning deletes the objects it generated and rebuilds them.
    /// </summary>
    public static class FeedingUISetup
    {
        private const string StagePrefabPath = "Assets/__Project/Resources/Prefabs/InventoryStage.prefab";
        private const string HudPrefabPath = "Assets/__Project/Resources/Prefabs/UI/InventoryHud.prefab";
        private const string BubblePrefabPath = "Assets/__Project/Resources/Prefabs/UI/Bubble.prefab";

        private const int InventoryFocusLayer = 8;
        private const int UiLayer = 5;

        // Generated root objects (named so reruns can find and remove them).
        private const string FeedingAreaName = "FeedingArea";
        private const string FeedModeButtonName = "FeedModeButton";

        // Match the crafting slot scale so feeding bubbles read at the same size.
        private const float TrayBubbleScale = 0.22f;
        private const float TraySpacing = 0.22f;

        [MenuItem("Tools/Inventory/Setup Feeding UI")]
        public static void SetupFeedingUI()
        {
            SetupStage();
            SetupHud();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FeedingUISetup] Feeding UI built on InventoryStage and InventoryHud prefabs.");
        }

        private static void SetupStage()
        {
            var root = PrefabUtility.LoadPrefabContents(StagePrefabPath);
            if (root == null)
            {
                Debug.LogError($"[FeedingUISetup] Could not load {StagePrefabPath}");
                return;
            }

            try
            {
                RemoveChild(root.transform, FeedingAreaName);

                var area = NewChild(FeedingAreaName, root.transform, InventoryFocusLayer);

                // Feeding slots sit below the pot (crafting slots are above, at y ~0.62).
                var trayAnchor = NewChild("FeedingTrayAnchor", area.transform, InventoryFocusLayer);
                trayAnchor.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                trayAnchor.transform.localScale = Vector3.one * TrayBubbleScale;

                BuildReadoutCanvas(
                    area.transform,
                    out GameObject panelRoot,
                    out Text cumulativeLabel,
                    out Text dominantLabel,
                    out Text progressLabel,
                    out Image progressFill,
                    out Button feedButton);

                var feedingView = area.AddComponent<FeedingView>();
                var bubblePrefab = AssetDatabase.LoadAssetAtPath<BubbleView>(BubblePrefabPath);
                if (bubblePrefab == null)
                {
                    Debug.LogWarning($"[FeedingUISetup] Bubble prefab not found at {BubblePrefabPath}; _artifactItemPrefab left empty.");
                }

                var so = new SerializedObject(feedingView);
                so.FindProperty("_trayAnchor").objectReferenceValue = trayAnchor.transform;
                so.FindProperty("_artifactItemPrefab").objectReferenceValue = bubblePrefab;
                so.FindProperty("_traySpacing").floatValue = TraySpacing;
                so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
                so.FindProperty("_feedButton").objectReferenceValue = feedButton;
                so.FindProperty("_cumulativeLabel").objectReferenceValue = cumulativeLabel;
                so.FindProperty("_dominantLabel").objectReferenceValue = dominantLabel;
                so.FindProperty("_progressLabel").objectReferenceValue = progressLabel;
                so.FindProperty("_progressFill").objectReferenceValue = progressFill;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, StagePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupHud()
        {
            var root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[FeedingUISetup] Could not load {HudPrefabPath}");
                return;
            }

            try
            {
                var hudView = root.GetComponent<InventoryHudView>();
                if (hudView == null)
                {
                    Debug.LogError("[FeedingUISetup] InventoryHud prefab has no InventoryHudView.");
                    return;
                }

                RemoveChild(root.transform, FeedModeButtonName);

                // Sits above the OpenButton (bottom-right, (-30, 30), size 130).
                var buttonGo = NewUiChild(FeedModeButtonName, root.transform);
                var rect = (RectTransform)buttonGo.transform;
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-30f, 170f);
                rect.sizeDelta = new Vector2(130f, 130f);

                var image = buttonGo.AddComponent<Image>();
                image.sprite = BuiltinSprite();
                image.type = Image.Type.Sliced;
                image.color = new Color(0.4f, 0.5f, 0.65f, 0.9f);

                var button = buttonGo.AddComponent<Button>();
                button.targetGraphic = image;

                AddLabel(buttonGo.transform, "Feed", 30);

                // Small highlight shown while feeding mode is active (toggled by the view).
                var indicator = NewUiChild("ActiveIndicator", buttonGo.transform);
                var indicatorRect = (RectTransform)indicator.transform;
                indicatorRect.anchorMin = Vector2.zero;
                indicatorRect.anchorMax = Vector2.one;
                indicatorRect.offsetMin = new Vector2(-6f, -6f);
                indicatorRect.offsetMax = new Vector2(6f, 6f);
                var indicatorImage = indicator.AddComponent<Image>();
                indicatorImage.sprite = BuiltinSprite();
                indicatorImage.type = Image.Type.Sliced;
                indicatorImage.color = new Color(1f, 0.9f, 0.3f, 0.6f);
                indicator.transform.SetAsFirstSibling();
                indicator.SetActive(false);

                var so = new SerializedObject(hudView);
                so.FindProperty("_feedModeButton").objectReferenceValue = button;
                so.FindProperty("_feedModeActiveIndicator").objectReferenceValue = indicator;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildReadoutCanvas(
            Transform parent,
            out GameObject panelRoot,
            out Text cumulativeLabel,
            out Text dominantLabel,
            out Text progressLabel,
            out Image progressFill,
            out Button feedButton)
        {
            // Explicit RectTransform so the Canvas never relies on AddComponent ordering.
            var canvasGo = new GameObject(
                "FeedingReadoutCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.layer = UiLayer;
            canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Below the HUD (sortingOrder 50) so its open/close/feed buttons stay clickable on top.
            canvas.sortingOrder = 40;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel anchored to the right side of the (screen-centered) pot.
            panelRoot = NewUiChild("PanelRoot", canvasGo.transform);
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-40f, 0f);
            panelRect.sizeDelta = new Vector2(420f, 460f);

            var panelBg = panelRoot.AddComponent<Image>();
            panelBg.sprite = BuiltinSprite();
            panelBg.type = Image.Type.Sliced;
            panelBg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            var layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            layout.padding = new RectOffset(16, 16, 16, 16);

            AddHeader(panelRoot.transform, "Feeding");
            cumulativeLabel = AddReadoutText(panelRoot.transform, "Nothing selected", 70f);
            dominantLabel = AddReadoutText(panelRoot.transform, "No archetype yet", 70f);
            progressLabel = AddReadoutText(panelRoot.transform, "Digestion: 0/0", 30f);
            progressFill = AddProgressBar(panelRoot.transform);
            feedButton = AddFeedButton(panelRoot.transform);
        }

        private static void AddHeader(Transform parent, string text)
        {
            var go = NewUiChild("Header", parent);
            var label = go.AddComponent<Text>();
            ConfigureText(label, text, 32, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            AddLayoutHeight(go, 40f);
        }

        private static Text AddReadoutText(Transform parent, string placeholder, float height)
        {
            var go = NewUiChild("Label", parent);
            var label = go.AddComponent<Text>();
            ConfigureText(label, placeholder, 26, TextAnchor.UpperLeft);
            AddLayoutHeight(go, height);
            return label;
        }

        private static Image AddProgressBar(Transform parent)
        {
            var barGo = NewUiChild("ProgressBar", parent);
            var barBg = barGo.AddComponent<Image>();
            barBg.sprite = BuiltinSprite();
            barBg.type = Image.Type.Sliced;
            barBg.color = new Color(0f, 0f, 0f, 0.5f);
            AddLayoutHeight(barGo, 28f);

            var fillGo = NewUiChild("Fill", barGo.transform);
            var fillRect = (RectTransform)fillGo.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fill = fillGo.AddComponent<Image>();
            fill.sprite = BuiltinSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = new Color(0.4f, 0.8f, 0.5f, 1f);
            return fill;
        }

        private static Button AddFeedButton(Transform parent)
        {
            var go = NewUiChild("FeedButton", parent);
            var image = go.AddComponent<Image>();
            image.sprite = BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.6f, 0.35f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            AddLayoutHeight(go, 64f);

            AddLabel(go.transform, "Feed", 28);
            return button;
        }

        private static void AddLabel(Transform parent, string text, int fontSize)
        {
            var go = NewUiChild("Label", parent);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.AddComponent<Text>();
            ConfigureText(label, text, fontSize, TextAnchor.MiddleCenter);
        }

        private static void ConfigureText(Text label, string text, int fontSize, TextAnchor anchor)
        {
            label.text = text;
            label.font = BuiltinFont();
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.color = Color.white;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void AddLayoutHeight(GameObject go, float preferredHeight)
        {
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
        }

        private static GameObject NewChild(string name, Transform parent, int layer)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static GameObject NewUiChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Font BuiltinFont()
        {
            // Unity 2022+/6 ship LegacyRuntime.ttf in place of the old Arial.ttf.
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}
