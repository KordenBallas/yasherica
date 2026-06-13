using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Inspector for SkeletonDefinition: "Sync Bone Names From Rig Prefab" keeps the
    /// authoritative bone list in lockstep with the actual rig hierarchy, plus inline
    /// validation (duplicate socket ids, sockets on missing bones).
    /// </summary>
    [CustomEditor(typeof(SkeletonDefinition))]
    public class SkeletonDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var skeleton = (SkeletonDefinition)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Sync Bone Names From Rig Prefab"))
            {
                SyncBoneNames(skeleton);
            }

            DrawValidation(skeleton);
        }

        private void SyncBoneNames(SkeletonDefinition skeleton)
        {
            if (skeleton.RigPrefab == null)
            {
                EditorUtility.DisplayDialog("Sync Bone Names", "Assign a rig prefab first.", "OK");
                return;
            }

            var rig = skeleton.RigPrefab.GetComponent<CharacterRig>();
            if (rig == null)
            {
                EditorUtility.DisplayDialog("Sync Bone Names", "The rig prefab has no CharacterRig component on its root.", "OK");
                return;
            }

            // Bones are every transform under the rig root (the root GameObject itself
            // holds the Animator and is not a bone) — same rule CharacterRig.Initialize uses.
            var boneNames = new List<string>();
            for (var i = 0; i < skeleton.RigPrefab.transform.childCount; i++)
            {
                CollectNamesRecursive(skeleton.RigPrefab.transform.GetChild(i), boneNames);
            }

            var boneNamesProperty = serializedObject.FindProperty("_boneNames");
            boneNamesProperty.arraySize = boneNames.Count;
            for (var i = 0; i < boneNames.Count; i++)
            {
                boneNamesProperty.GetArrayElementAtIndex(i).stringValue = boneNames[i];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void CollectNamesRecursive(Transform current, List<string> names)
        {
            names.Add(current.name);
            for (var i = 0; i < current.childCount; i++)
            {
                CollectNamesRecursive(current.GetChild(i), names);
            }
        }

        private static void DrawValidation(SkeletonDefinition skeleton)
        {
            EditorGUILayout.Space();

            if (skeleton.RigPrefab == null)
            {
                EditorGUILayout.HelpBox("No rig prefab assigned.", MessageType.Error);
            }

            if (skeleton.BoneNames.Count == 0)
            {
                EditorGUILayout.HelpBox("Bone name list is empty; use 'Sync Bone Names From Rig Prefab'.", MessageType.Error);
                return;
            }

            var issues = new AssemblyValidator().ValidateSkeleton(DefinitionMapper.ToSkeletonData(skeleton));
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Skeleton definition is valid.", MessageType.None);
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
