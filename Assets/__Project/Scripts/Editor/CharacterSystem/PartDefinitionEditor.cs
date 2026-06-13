using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Inspector for PartDefinition: inline validation against the target skeleton and
    /// the "Bake Bone Names From Prefab" button, which records the SkinnedMeshRenderer's
    /// bone names in exact bone-index order — the ingestion path for rigged FBX art.
    /// </summary>
    [CustomEditor(typeof(PartDefinition))]
    public class PartDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var part = (PartDefinition)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake Bone Names From Prefab"))
            {
                BakeBoneNames(part);
            }

            DrawValidation(part);
        }

        private void BakeBoneNames(PartDefinition part)
        {
            if (part.PartPrefab == null)
            {
                EditorUtility.DisplayDialog("Bake Bone Names", "Assign a part prefab first.", "OK");
                return;
            }

            var renderer = part.PartPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Bake Bone Names", "The part prefab has no SkinnedMeshRenderer.", "OK");
                return;
            }

            var bones = renderer.bones;
            if (bones == null || bones.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Bake Bone Names",
                    "The prefab's SkinnedMeshRenderer has no bones assigned. Baking only works on rigged source prefabs (e.g. imported FBX); generated placeholder parts already carry their bone lists.",
                    "OK");
                return;
            }

            var boneNamesProperty = serializedObject.FindProperty("_boneNames");
            boneNamesProperty.arraySize = bones.Length;
            for (var i = 0; i < bones.Length; i++)
            {
                boneNamesProperty.GetArrayElementAtIndex(i).stringValue = bones[i] != null ? bones[i].name : string.Empty;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawValidation(PartDefinition part)
        {
            EditorGUILayout.Space();

            if (part.Slot == null)
            {
                EditorGUILayout.HelpBox("No slot assigned.", MessageType.Error);
            }

            if (part.PartPrefab == null)
            {
                EditorGUILayout.HelpBox("No part prefab assigned.", MessageType.Error);
            }

            if (part.TargetSkeleton == null)
            {
                EditorGUILayout.HelpBox("No target skeleton assigned; bone validation is unavailable.", MessageType.Warning);
                return;
            }

            var issues = new AssemblyValidator().ValidatePart(
                DefinitionMapper.ToPartData(part),
                DefinitionMapper.ToSkeletonData(part.TargetSkeleton));

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Part is valid for its target skeleton.", MessageType.None);
                return;
            }

            foreach (var issue in issues)
            {
                EditorGUILayout.HelpBox(issue.ToString(),
                    issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
            }
        }
    }
}
