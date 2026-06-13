using System;
using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using Core.Logging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Designer-facing assembly preview: pick a skeleton (or preload an assembly),
    /// choose a part per slot, and build an editable scene instance through the SAME
    /// ModularCharacterFactory used at runtime. Includes socket gizmo display and an
    /// AssemblyValidator issue panel.
    /// </summary>
    public class CharacterAssemblyPreviewWindow : EditorWindow
    {
        private SkeletonDefinition _skeleton;
        private CharacterAssemblyDefinition _assemblyToLoad;

        private readonly Dictionary<string, PartDefinition> _selectedParts = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);
        private List<PartDefinition> _allParts = new List<PartDefinition>();

        private GameObject _previewInstance;
        private bool _showSockets = true;
        private Vector2 _scroll;
        private IReadOnlyList<ValidationIssue> _issues = Array.Empty<ValidationIssue>();

        [MenuItem("Tools/Character System/Assembly Preview")]
        public static void Open()
        {
            GetWindow<CharacterAssemblyPreviewWindow>("Assembly Preview");
        }

        private void OnEnable()
        {
            RefreshPartList();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearPreview();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSourceSection();

            if (_skeleton != null)
            {
                DrawSlotSection();
                DrawActionsSection();
                DrawValidationSection();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Skeleton Definition (or load an assembly) to start.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceSection()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _skeleton = (SkeletonDefinition)EditorGUILayout.ObjectField("Skeleton", _skeleton, typeof(SkeletonDefinition), false);
                if (check.changed)
                {
                    _selectedParts.Clear();
                    RefreshPartList();
                }
            }

            EditorGUILayout.BeginHorizontal();
            _assemblyToLoad = (CharacterAssemblyDefinition)EditorGUILayout.ObjectField("Load Assembly", _assemblyToLoad, typeof(CharacterAssemblyDefinition), false);
            using (new EditorGUI.DisabledScope(_assemblyToLoad == null))
            {
                if (GUILayout.Button("Load", GUILayout.Width(60f)))
                {
                    LoadAssembly(_assemblyToLoad);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawSlotSection()
        {
            EditorGUILayout.LabelField("Parts Per Slot", EditorStyles.boldLabel);

            foreach (var slot in CollectSlotsForSkeleton())
            {
                var candidates = CollectPartsForSlot(slot);
                var labels = new string[candidates.Count + 1];
                labels[0] = "(none)";
                for (var i = 0; i < candidates.Count; i++)
                {
                    labels[i + 1] = candidates[i].Id;
                }

                _selectedParts.TryGetValue(slot.Id, out var current);
                var currentIndex = current != null ? candidates.IndexOf(current) + 1 : 0;
                var newIndex = EditorGUILayout.Popup(slot.DisplayName, currentIndex, labels);

                if (newIndex <= 0)
                {
                    _selectedParts.Remove(slot.Id);
                }
                else
                {
                    _selectedParts[slot.Id] = candidates[newIndex - 1];
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawActionsSection()
        {
            _showSockets = EditorGUILayout.Toggle("Show Sockets", _showSockets);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build Preview"))
            {
                BuildPreview();
            }

            using (new EditorGUI.DisabledScope(_previewInstance == null))
            {
                if (GUILayout.Button("Clear"))
                {
                    ClearPreview();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Current Selection"))
            {
                RunValidation();
            }

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.None);
                return;
            }

            foreach (var issue in _issues)
            {
                EditorGUILayout.HelpBox(issue.ToString(),
                    issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
            }
        }

        private void LoadAssembly(CharacterAssemblyDefinition assembly)
        {
            _skeleton = assembly.Skeleton;
            _selectedParts.Clear();
            RefreshPartList();

            foreach (var part in assembly.Parts)
            {
                if (part != null && part.Slot != null)
                {
                    _selectedParts[part.Slot.Id] = part;
                }
            }
        }

        private void BuildPreview()
        {
            ClearPreview();
            RunValidation();

            // Transient, never saved as an asset: just a container for the factory call,
            // so the preview exercises exactly the runtime assembly path.
            var assembly = ScriptableObject.CreateInstance<CharacterAssemblyDefinition>();
            var serialized = new SerializedObject(assembly);
            serialized.FindProperty("_id").stringValue = "preview";
            serialized.FindProperty("_skeleton").objectReferenceValue = _skeleton;
            var partsProperty = serialized.FindProperty("_parts");
            partsProperty.arraySize = _selectedParts.Count;
            var index = 0;
            foreach (var part in _selectedParts.Values)
            {
                partsProperty.GetArrayElementAtIndex(index++).objectReferenceValue = part;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            var logger = new UnityGameLogger();
            var factory = new ModularCharacterFactory(new PartCatalog(_allParts), logger);
            var character = factory.Create(assembly, null);

            DestroyImmediate(assembly);

            if (character == null)
            {
                return;
            }

            _previewInstance = character.gameObject;
            _previewInstance.name = "[Character Preview]";
            _previewInstance.hideFlags = HideFlags.DontSave;
            Selection.activeGameObject = _previewInstance;
            SceneView.RepaintAll();
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }
        }

        private void RunValidation()
        {
            if (_skeleton == null)
            {
                _issues = Array.Empty<ValidationIssue>();
                return;
            }

            var skeletonData = DefinitionMapper.ToSkeletonData(_skeleton);
            var parts = new List<PartData>(_selectedParts.Count);
            foreach (var part in _selectedParts.Values)
            {
                parts.Add(DefinitionMapper.ToPartData(part));
            }

            _issues = new AssemblyValidator().ValidateAssembly(skeletonData, parts);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showSockets)
            {
                return;
            }

            DrawSocketsFor(_previewInstance);

            // Also draw sockets for any selected runtime character (e.g. in play mode).
            if (Selection.activeGameObject != null && Selection.activeGameObject != _previewInstance)
            {
                DrawSocketsFor(Selection.activeGameObject);
            }
        }

        private static void DrawSocketsFor(GameObject candidate)
        {
            if (candidate == null)
            {
                return;
            }

            var character = candidate.GetComponent<ModularCharacter>();
            if (character != null)
            {
                SocketHandleDrawer.DrawSockets(character);
            }
        }

        private void RefreshPartList()
        {
            _allParts = new List<PartDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:PartDefinition"))
            {
                var part = AssetDatabase.LoadAssetAtPath<PartDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (part != null)
                {
                    _allParts.Add(part);
                }
            }
        }

        private List<SlotDefinition> CollectSlotsForSkeleton()
        {
            var slots = new List<SlotDefinition>();
            foreach (var part in _allParts)
            {
                if (part.TargetSkeleton == _skeleton && part.Slot != null && !slots.Contains(part.Slot))
                {
                    slots.Add(part.Slot);
                }
            }

            slots.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return slots;
        }

        private List<PartDefinition> CollectPartsForSlot(SlotDefinition slot)
        {
            var parts = new List<PartDefinition>();
            foreach (var part in _allParts)
            {
                if (part.TargetSkeleton == _skeleton && part.Slot == slot)
                {
                    parts.Add(part);
                }
            }

            parts.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return parts;
        }
    }
}
