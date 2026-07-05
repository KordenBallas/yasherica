using CharacterSystem.View;
using Core.DI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// One-click build of the body-plan demo scene: a small walled platform, the Hero prefab
    /// (movement + modular visual + locomotion), and the part-selection dev console — one
    /// dropdown per slot, installs routed through the body-plan coordinator so frame changes
    /// run the real confirm-and-shed flow. Saved to Assets/__Project/Scenes/BodyPlanDemo.unity.
    /// Requires the placeholder assets (Tools/Character System/Generate Placeholder Assets).
    /// </summary>
    public static class BodyPlanDemoSceneBuilder
    {
        private const string ScenePath = "Assets/__Project/Scenes/BodyPlanDemo.unity";
        private const string HeroPrefabPath = "Assets/__Project/Resources/Prefabs/Hero.prefab";
        private const string AssemblyPath = "Assets/__Project/Resources/CharacterSystem/Assemblies/PlaceholderAssembly_A.asset";

        private const float PlatformSize = 14f;
        private const float WallHeight = 3f;
        // The hero prefab's wallLayer mask is bit 512 = layer 9; walls must sit on it so
        // dash raycasts see them (colliders alone only stop CharacterController.Move).
        private const int WallLayer = 9;

        [MenuItem("Tools/Character System/Build Body-Plan Demo Scene")]
        public static void BuildScene()
        {
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            if (heroPrefab == null)
            {
                Debug.LogError($"[BodyPlanDemoSceneBuilder] Hero prefab not found at {HeroPrefabPath}.");
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(AssemblyPath) == null)
            {
                Debug.LogError(
                    "[BodyPlanDemoSceneBuilder] Placeholder assets missing. Run Tools/Character System/Generate Placeholder Assets first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetUpCamera();
            BuildPlatform();

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
            hero.transform.position = new Vector3(0f, 1.2f, 0f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            BuildConsole();
            BuildSceneContext();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BodyPlanDemoSceneBuilder] Demo scene saved to {ScenePath}. Press Play: WASD to move, dropdowns to swap parts; frame-changing picks open the confirm dialog.");
        }

        private static void SetUpCamera()
        {
            var camera = Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(0f, 12f, -11f);
            camera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
        }

        private static void BuildPlatform()
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.position = new Vector3(0f, -0.25f, 0f);
            platform.transform.localScale = new Vector3(PlatformSize, 0.5f, PlatformSize);

            var half = PlatformSize / 2f;
            CreateWall("Wall_North", new Vector3(0f, WallHeight / 2f, half + 0.25f), new Vector3(PlatformSize + 1f, WallHeight, 0.5f));
            CreateWall("Wall_South", new Vector3(0f, WallHeight / 2f, -half - 0.25f), new Vector3(PlatformSize + 1f, WallHeight, 0.5f));
            CreateWall("Wall_East", new Vector3(half + 0.25f, WallHeight / 2f, 0f), new Vector3(0.5f, WallHeight, PlatformSize + 1f));
            CreateWall("Wall_West", new Vector3(-half - 0.25f, WallHeight / 2f, 0f), new Vector3(0.5f, WallHeight, PlatformSize + 1f));
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.layer = WallLayer;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            // Invisible fence: collider only, so the hero cannot walk or dash off the platform.
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(wall.GetComponent<MeshFilter>());
        }

        // ----- Console UI ---------------------------------------------------

        private static void BuildConsole()
        {
            var canvasObject = new GameObject("BodyPlanDemoConsole", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = CreateUiObject("Panel", canvasObject.transform, out var panelRect);
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(16f, 0f);
            panelRect.sizeDelta = new Vector2(380f, -32f);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateHeader(panel.transform);
            var rowContainer = CreateRowContainer(panel.transform);
            var rowTemplate = CreateRowTemplate(panel.transform);

            var view = panel.AddComponent<BodyPlanDemoConsoleView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_rowContainer").objectReferenceValue = rowContainer;
            serialized.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHeader(Transform parent)
        {
            var header = CreateUiObject("Header", parent, out _);
            var text = header.AddComponent<TextMeshProUGUI>();
            text.text = "Body-plan console";
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;

            var element = header.AddComponent<LayoutElement>();
            element.preferredHeight = 32f;
        }

        private static RectTransform CreateRowContainer(Transform parent)
        {
            var container = CreateUiObject("Rows", parent, out var rect);
            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var element = container.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            return rect;
        }

        private static GameObject CreateRowTemplate(Transform parent)
        {
            var row = CreateUiObject("RowTemplate", parent, out _);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 36f;

            // The view finds this child by name — keep "Label".
            var label = CreateUiObject("Label", row.transform, out _);
            var labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Slot";
            labelText.fontSize = 16f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            var labelElement = label.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 110f;

            var dropdownObject = TMP_DefaultControls.CreateDropdown(BuiltinUiResources());
            dropdownObject.name = "Dropdown";
            dropdownObject.transform.SetParent(row.transform, false);
            var dropdownElement = dropdownObject.AddComponent<LayoutElement>();
            dropdownElement.flexibleWidth = 1f;
            dropdownElement.preferredHeight = 32f;

            row.SetActive(false);
            return row;
        }

        private static GameObject CreateUiObject(string name, Transform parent, out RectTransform rect)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.layer = LayerMask.NameToLayer("UI");
            rect = uiObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return uiObject;
        }

        private static TMP_DefaultControls.Resources BuiltinUiResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        // ----- DI -----------------------------------------------------------

        private static void BuildSceneContext()
        {
            var contextObject = new GameObject("SceneContext");
            var sceneContext = contextObject.AddComponent<SceneContext>();

            var characterSystem = contextObject.AddComponent<CharacterSystemInstaller>();
            var locomotion = contextObject.AddComponent<CharacterLocomotionInstaller>();
            var demo = contextObject.AddComponent<BodyPlanDemoInstaller>();

            // Assign through the public property (its setter writes the serialized
            // _monoInstallers list; FindProperty("_installers") would silently miss it).
            sceneContext.Installers = new MonoInstaller[] { characterSystem, locomotion, demo };
            EditorUtility.SetDirty(sceneContext);
        }
    }
}
