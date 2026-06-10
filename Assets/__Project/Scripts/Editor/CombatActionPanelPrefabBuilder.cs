using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Combat.View;
using System.IO;

namespace Editor
{
    /// <summary>
    /// Editor utility to programmatically create the CombatActionPanel UI prefab.
    /// Builds the complete hierarchy, wires all serialized fields, and optionally places in scene.
    /// </summary>
    public static class CombatActionPanelPrefabBuilder
    {
        private const string PrefabPath = "Assets/__Project/Resources/Prefabs/UI/CombatActionPanel.prefab";

        [MenuItem("Tools/Combat/Create Action Panel Prefab")]
        public static void CreatePrefab()
        {
            BuildAndSavePrefab(placeInScene: false);
        }

        [MenuItem("Tools/Combat/Create Action Panel Prefab And Place In Scene")]
        public static void CreatePrefabAndPlaceInScene()
        {
            BuildAndSavePrefab(placeInScene: true);
        }

        private static void BuildAndSavePrefab(bool placeInScene)
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(PrefabPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            // Build the UI hierarchy
            GameObject root = BuildUIHierarchy();

            // Save as prefab
            bool isNewPrefab;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out isNewPrefab);

            if (prefab != null)
            {
                Debug.Log($"CombatActionPanel prefab {(isNewPrefab ? "created" : "updated")} at: {PrefabPath}");

                if (placeInScene)
                {
                    // Place instance in scene
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    instance.name = "CombatActionPanel";
                    Selection.activeGameObject = instance;
                    Debug.Log("CombatActionPanel instance placed in scene and selected.");
                }
                else
                {
                    // Clean up temporary root object
                    Object.DestroyImmediate(root);

                    // Select the prefab asset
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }
            else
            {
                Debug.LogError("Failed to save CombatActionPanel prefab.");
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildUIHierarchy()
        {
            // Root Canvas
            GameObject canvasGO = new GameObject("CombatActionPanel");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel Root
            GameObject panelRoot = CreateGameObject("PanelRoot", canvasGO.transform);
            RectTransform panelRootRT = panelRoot.GetComponent<RectTransform>();
            panelRootRT.anchorMin = new Vector2(0.1f, 0f);
            panelRootRT.anchorMax = new Vector2(0.9f, 0f);
            panelRootRT.pivot = new Vector2(0.5f, 0f);
            panelRootRT.sizeDelta = new Vector2(0f, 140f);
            panelRootRT.anchoredPosition = Vector2.zero;

            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(20f/255f, 20f/255f, 20f/255f, 220f/255f);

            VerticalLayoutGroup panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = true;
            panelLayout.spacing = 8f;
            panelLayout.padding = new RectOffset(10, 10, 10, 10);

            // Action Buttons Row
            GameObject actionRow = CreateGameObject("ActionButtonsRow", panelRoot.transform);
            HorizontalLayoutGroup actionRowLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionRowLayout.childControlWidth = true;
            actionRowLayout.childControlHeight = true;
            actionRowLayout.childForceExpandWidth = true;
            actionRowLayout.childForceExpandHeight = false;
            actionRowLayout.spacing = 8f;
            actionRowLayout.childAlignment = TextAnchor.MiddleCenter;

            LayoutElement actionRowLE = actionRow.AddComponent<LayoutElement>();
            actionRowLE.preferredHeight = 40f;
            actionRowLE.flexibleHeight = 0f;

            // Move Button
            var moveButton = CreateActionButton("MoveButton", actionRow.transform, "Move", "A");

            // Direction Button
            var directionButton = CreateActionButton("DirectionButton", actionRow.transform, "Direction", "S");

            // Execute Button (has extra queue count text)
            var executeButton = CreateActionButton("ExecuteButton", actionRow.transform, "Execute", "Enter");
            GameObject queueCountTextGO = CreateTMPText("QueueCount", executeButton.root.transform, "", 14);
            RectTransform queueCountRT = queueCountTextGO.GetComponent<RectTransform>();
            queueCountRT.anchorMin = new Vector2(0.7f, 0.5f);
            queueCountRT.anchorMax = new Vector2(1f, 0.5f);
            queueCountRT.pivot = new Vector2(1f, 0.5f);
            queueCountRT.sizeDelta = new Vector2(0f, 20f);
            queueCountRT.anchoredPosition = new Vector2(-5f, 0f);
            TextMeshProUGUI queueCountText = queueCountTextGO.GetComponent<TextMeshProUGUI>();
            queueCountText.alignment = TextAlignmentOptions.MidlineRight;
            queueCountText.color = Color.yellow;

            // Ability Slots Row
            GameObject abilityRow = CreateGameObject("AbilitySlotsRow", panelRoot.transform);
            HorizontalLayoutGroup abilityRowLayout = abilityRow.AddComponent<HorizontalLayoutGroup>();
            abilityRowLayout.childControlWidth = true;
            abilityRowLayout.childControlHeight = true;
            abilityRowLayout.childForceExpandWidth = true;
            abilityRowLayout.childForceExpandHeight = true;
            abilityRowLayout.spacing = 8f;
            abilityRowLayout.childAlignment = TextAnchor.MiddleCenter;

            // Create 5 ability slots
            var abilitySlots = new CombatActionPanelView.AbilitySlot[5];
            for (int i = 0; i < 5; i++)
            {
                abilitySlots[i] = CreateAbilitySlot($"AbilitySlot_{i}", abilityRow.transform, $"{i + 1}");
            }

            // Add CombatActionPanelView component and wire all fields
            CombatActionPanelView view = canvasGO.AddComponent<CombatActionPanelView>();
            WireSerializedFields(view, panelRoot, moveButton, directionButton, executeButton, queueCountText, abilitySlots);

            return canvasGO;
        }

        private static void WireSerializedFields(
            CombatActionPanelView view,
            GameObject panelRoot,
            ActionButtonData moveButton,
            ActionButtonData directionButton,
            ActionButtonData executeButton,
            TextMeshProUGUI queueCountText,
            CombatActionPanelView.AbilitySlot[] abilitySlots)
        {
            SerializedObject so = new SerializedObject(view);

            // Wire panel root
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;

            // Wire action buttons
            so.FindProperty("_moveButton").objectReferenceValue = moveButton.button;
            so.FindProperty("_moveKeybindText").objectReferenceValue = moveButton.keybindText;
            so.FindProperty("_changeDirectionButton").objectReferenceValue = directionButton.button;
            so.FindProperty("_changeDirectionKeybindText").objectReferenceValue = directionButton.keybindText;
            so.FindProperty("_executeQueueButton").objectReferenceValue = executeButton.button;
            so.FindProperty("_executeQueueKeybindText").objectReferenceValue = executeButton.keybindText;
            so.FindProperty("_queueCountText").objectReferenceValue = queueCountText;

            // Wire ability slots array
            SerializedProperty slotsProp = so.FindProperty("_abilitySlots");
            slotsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                SerializedProperty slotElement = slotsProp.GetArrayElementAtIndex(i);
                slotElement.FindPropertyRelative("Button").objectReferenceValue = abilitySlots[i].Button;
                slotElement.FindPropertyRelative("IconImage").objectReferenceValue = abilitySlots[i].IconImage;
                slotElement.FindPropertyRelative("NameText").objectReferenceValue = abilitySlots[i].NameText;
                slotElement.FindPropertyRelative("KeybindText").objectReferenceValue = abilitySlots[i].KeybindText;
                slotElement.FindPropertyRelative("CooldownText").objectReferenceValue = abilitySlots[i].CooldownText;
                slotElement.FindPropertyRelative("CooldownOverlay").objectReferenceValue = abilitySlots[i].CooldownOverlay;
                slotElement.FindPropertyRelative("HighlightImage").objectReferenceValue = abilitySlots[i].HighlightImage;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateGameObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        private static GameObject CreateTMPText(string name, Transform parent, string text, int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private struct ActionButtonData
        {
            public GameObject root;
            public Button button;
            public TextMeshProUGUI keybindText;
        }

        private static ActionButtonData CreateActionButton(string name, Transform parent, string labelText, string keybindText)
        {
            ActionButtonData data = new ActionButtonData();

            // Button root
            data.root = new GameObject(name);
            data.root.transform.SetParent(parent, false);
            RectTransform buttonRT = data.root.AddComponent<RectTransform>();

            Image buttonImage = data.root.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            data.button = data.root.AddComponent<Button>();
            data.button.targetGraphic = buttonImage;

            ColorBlock colors = data.button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            data.button.colors = colors;

            // Label
            GameObject labelGO = CreateTMPText("Label", data.root.transform, labelText, 14);
            RectTransform labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0.7f, 1f);

            // Keybind text
            GameObject keybindGO = CreateTMPText("KeybindText", data.root.transform, keybindText, 12);
            RectTransform keybindRT = keybindGO.GetComponent<RectTransform>();
            keybindRT.anchorMin = new Vector2(0.7f, 0f);
            keybindRT.anchorMax = new Vector2(1f, 1f);

            TextMeshProUGUI keybindTMP = keybindGO.GetComponent<TextMeshProUGUI>();
            keybindTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            data.keybindText = keybindTMP;

            return data;
        }

        private static CombatActionPanelView.AbilitySlot CreateAbilitySlot(string name, Transform parent, string keybindText)
        {
            CombatActionPanelView.AbilitySlot slot = new CombatActionPanelView.AbilitySlot();

            // Slot button root
            GameObject slotGO = new GameObject(name);
            slotGO.transform.SetParent(parent, false);
            RectTransform slotRT = slotGO.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(100f, 80f);

            LayoutElement slotLE = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth = 100f;
            slotLE.preferredHeight = 80f;
            slotLE.flexibleWidth = 0f;

            Image slotBg = slotGO.AddComponent<Image>();
            slotBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            Button button = slotGO.AddComponent<Button>();
            button.targetGraphic = slotBg;
            slot.Button = button;

            // Icon Image (centered, 50x50)
            GameObject iconGO = new GameObject("IconImage");
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(50f, 50f);
            iconRT.anchoredPosition = Vector2.zero;

            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            slot.IconImage = iconImage;

            // Name Text (bottom strip)
            GameObject nameGO = CreateTMPText("NameText", slotGO.transform, "", 10);
            RectTransform nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 0.2f);
            nameRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.alignment = TextAlignmentOptions.Bottom;
            slot.NameText = nameTMP;

            // Keybind Text (top-right corner)
            GameObject keybindGO = CreateTMPText("KeybindText", slotGO.transform, keybindText, 9);
            RectTransform keybindRT = keybindGO.GetComponent<RectTransform>();
            keybindRT.anchorMin = new Vector2(0.7f, 0.8f);
            keybindRT.anchorMax = new Vector2(1f, 1f);
            keybindRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI keybindTMP = keybindGO.GetComponent<TextMeshProUGUI>();
            keybindTMP.alignment = TextAlignmentOptions.TopRight;
            keybindTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            slot.KeybindText = keybindTMP;

            // Cooldown Overlay (dark semi-transparent, starts inactive)
            GameObject overlayGO = new GameObject("CooldownOverlay");
            overlayGO.transform.SetParent(slotGO.transform, false);
            RectTransform overlayRT = overlayGO.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;

            Image overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.7f);
            slot.CooldownOverlay = overlayImage;
            overlayGO.SetActive(false);

            // Cooldown Text (centered, large, starts inactive)
            GameObject cooldownTextGO = CreateTMPText("CooldownText", slotGO.transform, "", 24);
            RectTransform cooldownRT = cooldownTextGO.GetComponent<RectTransform>();
            cooldownRT.anchorMin = Vector2.zero;
            cooldownRT.anchorMax = Vector2.one;
            cooldownRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI cooldownTMP = cooldownTextGO.GetComponent<TextMeshProUGUI>();
            cooldownTMP.alignment = TextAlignmentOptions.Center;
            cooldownTMP.fontStyle = FontStyles.Bold;
            cooldownTMP.color = Color.white;
            slot.CooldownText = cooldownTMP;
            cooldownTextGO.SetActive(false);

            // Highlight Image (yellow border, starts inactive)
            GameObject highlightGO = new GameObject("HighlightImage");
            highlightGO.transform.SetParent(slotGO.transform, false);
            RectTransform highlightRT = highlightGO.AddComponent<RectTransform>();
            highlightRT.anchorMin = Vector2.zero;
            highlightRT.anchorMax = Vector2.one;
            highlightRT.sizeDelta = Vector2.zero;

            Image highlightImage = highlightGO.AddComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 0f, 0.5f);
            slot.HighlightImage = highlightImage;
            highlightGO.SetActive(false);

            return slot;
        }
    }
}
