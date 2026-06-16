using Mutation.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor.Mutation
{
    /// <summary>
    /// One-click setup for the stage-up mutation choice UI. Builds two standalone prefab assets under
    /// <c>Resources/Prefabs/UI/</c>:
    ///  - <c>MutationChoiceButton.prefab</c> — one choice button (label + icon + tint) wired to a
    ///    <see cref="MutationChoiceButton"/>.
    ///  - <c>MutationChoicePanel.prefab</c> — a screen-space overlay canvas with a centered panel and a
    ///    horizontal anchor the presenter fills, wired to a <see cref="MutationChoiceView"/> on the root.
    /// The panel is a standalone prefab (not embedded in a scene) so `MutationInstaller` can instantiate
    /// it at runtime via Zenject `FromComponentInNewPrefab`, needing no scene authoring. Idempotent:
    /// rerunning overwrites the prefabs and removes any panel a previous version of this menu embedded
    /// in `InventoryStage`. Mirrors <c>Editor.Inventory.FeedingUISetup</c>.
    /// </summary>
    public static class MutationChoiceUISetup
    {
        private const string StagePrefabPath = "Assets/__Project/Resources/Prefabs/InventoryStage.prefab";
        private const string ButtonPrefabPath = "Assets/__Project/Resources/Prefabs/UI/MutationChoiceButton.prefab";
        private const string PanelPrefabPath = "Assets/__Project/Resources/Prefabs/UI/MutationChoicePanel.prefab";

        private const int UiLayer = 5;
        // Name a previous version of this menu used when it embedded the panel in InventoryStage.
        private const string LegacyChoiceAreaName = "MutationChoiceArea";

        [MenuItem("Tools/Mutation/Setup Stage-Up Choice UI")]
        public static void SetupChoiceUI()
        {
            EnsureFolder("Assets/__Project/Resources/Prefabs/UI");

            var buttonPrefab = BuildButtonPrefab();
            BuildPanelPrefab(buttonPrefab);
            RemoveLegacyEmbeddedPanel();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MutationChoiceUISetup] Built MutationChoicePanel + MutationChoiceButton prefabs " +
                      "under Resources/Prefabs/UI. Author ArchetypePartSetDefinition assets under " +
                      "Resources/Mutation/PartSets, then Play.");
        }

        private static void BuildPanelPrefab(MutationChoiceButton buttonPrefab)
        {
            // The canvas root carries the view; the presenter instantiates this whole prefab at runtime.
            var root = new GameObject(
                "MutationChoicePanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.layer = UiLayer;

            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                // Above the feeding readout (40), below the HUD buttons (50).
                canvas.sortingOrder = 45;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                // Centered panel, dim backdrop; toggled by the view.
                var panelRoot = NewUiChild("PanelRoot", root.transform);
                var panelRect = (RectTransform)panelRoot.transform;
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(720f, 360f);

                var panelBg = panelRoot.AddComponent<Image>();
                panelBg.sprite = BuiltinSprite();
                panelBg.type = Image.Type.Sliced;
                panelBg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

                var layout = panelRoot.AddComponent<VerticalLayoutGroup>();
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.spacing = 16f;
                layout.padding = new RectOffset(24, 24, 24, 24);

                AddHeader(panelRoot.transform, "Choose a mutation");

                // Buttons are laid out left-to-right under this anchor.
                var anchorGo = NewUiChild("ChoiceAnchor", panelRoot.transform);
                var anchorLayout = anchorGo.AddComponent<HorizontalLayoutGroup>();
                anchorLayout.childControlWidth = true;
                anchorLayout.childControlHeight = true;
                anchorLayout.childForceExpandWidth = true;
                anchorLayout.childForceExpandHeight = true;
                anchorLayout.spacing = 16f;
                AddLayoutHeight(anchorGo, 220f);

                var view = root.AddComponent<MutationChoiceView>();
                var so = new SerializedObject(view);
                so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
                so.FindProperty("_choiceAnchor").objectReferenceValue = anchorGo.transform;
                so.FindProperty("_choiceButtonPrefab").objectReferenceValue = buttonPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MutationChoiceButton BuildButtonPrefab()
        {
            var go = new GameObject("MutationChoiceButton", typeof(RectTransform));
            go.layer = UiLayer;

            MutationChoiceButton result;
            try
            {
                var image = go.AddComponent<Image>();
                image.sprite = BuiltinSprite();
                image.type = Image.Type.Sliced;
                image.color = new Color(0.3f, 0.45f, 0.6f, 1f);

                var button = go.AddComponent<Button>();
                button.targetGraphic = image;

                // Icon centered in the upper area; left empty until an option supplies one.
                var iconGo = NewUiChild("Icon", go.transform);
                var iconRect = (RectTransform)iconGo.transform;
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.anchoredPosition = new Vector2(0f, -16f);
                iconRect.sizeDelta = new Vector2(96f, 96f);
                var icon = iconGo.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.enabled = false;

                // Label across the bottom.
                var labelGo = NewUiChild("Label", go.transform);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition = new Vector2(0f, 16f);
                labelRect.sizeDelta = new Vector2(-16f, 64f);
                var label = labelGo.AddComponent<Text>();
                ConfigureText(label, "Mutation", 28, TextAnchor.LowerCenter);

                var choiceButton = go.AddComponent<MutationChoiceButton>();
                var so = new SerializedObject(choiceButton);
                so.FindProperty("_button").objectReferenceValue = button;
                so.FindProperty("_label").objectReferenceValue = label;
                so.FindProperty("_icon").objectReferenceValue = icon;
                so.FindProperty("_tintTarget").objectReferenceValue = image;
                so.ApplyModifiedPropertiesWithoutUndo();

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, ButtonPrefabPath);
                result = prefab.GetComponent<MutationChoiceButton>();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return result;
        }

        private static void RemoveLegacyEmbeddedPanel()
        {
            var root = PrefabUtility.LoadPrefabContents(StagePrefabPath);
            if (root == null)
            {
                return;
            }

            try
            {
                var existing = root.transform.Find(LegacyChoiceAreaName);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, StagePrefabPath);
                    Debug.Log("[MutationChoiceUISetup] Removed legacy embedded choice panel from InventoryStage.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddHeader(Transform parent, string text)
        {
            var go = NewUiChild("Header", parent);
            var label = go.AddComponent<Text>();
            ConfigureText(label, text, 34, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            AddLayoutHeight(go, 48f);
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

        private static GameObject NewUiChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/__Project/Resources/Prefabs", "UI");
            }
        }

        private static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}
