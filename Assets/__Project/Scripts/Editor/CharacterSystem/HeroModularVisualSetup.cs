using Character.Locomotion;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// One-click wiring of the player Hero prefab to use the modular character model:
    /// adds a ModularCharacterVisual, points it at the placeholder assembly, and hooks
    /// the capsule MeshRenderer so it is hidden at runtime. Edits the prefab through
    /// LoadPrefabContents/SaveAsPrefabAsset so no YAML is hand-edited.
    /// </summary>
    public static class HeroModularVisualSetup
    {
        private const string HeroPrefabPath = "Assets/__Project/Resources/Prefabs/Hero.prefab";
        private const string AssemblyPath = "Assets/__Project/Resources/CharacterSystem/Assemblies/PlaceholderAssembly_A.asset";

        [MenuItem("Tools/Character System/Attach Modular Visual To Hero")]
        public static void AttachModularVisualToHero()
        {
            var assembly = AssetDatabase.LoadAssetAtPath<CharacterAssemblyDefinition>(AssemblyPath);
            if (assembly == null)
            {
                Debug.LogError($"[HeroModularVisualSetup] Placeholder assembly not found at {AssemblyPath}. Run 'Generate Placeholder Assets' first.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(HeroPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[HeroModularVisualSetup] Could not load Hero prefab at {HeroPrefabPath}.");
                return;
            }

            try
            {
                var visual = root.GetComponent<ModularCharacterVisual>();
                if (visual == null)
                {
                    visual = root.AddComponent<ModularCharacterVisual>();
                }

                var serialized = new SerializedObject(visual);
                serialized.FindProperty("_assembly").objectReferenceValue = assembly;
                serialized.FindProperty("_placeholderRenderer").objectReferenceValue = root.GetComponent<MeshRenderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // The locomotion view drives the run blend + facing off the same rig; it belongs on
                // the Hero root next to the movement controller and is wired to this visual.
                var locomotionView = root.GetComponent<CharacterLocomotionView>();
                if (locomotionView == null)
                {
                    locomotionView = root.AddComponent<CharacterLocomotionView>();
                }

                var viewSerialized = new SerializedObject(locomotionView);
                viewSerialized.FindProperty("_characterVisual").objectReferenceValue = visual;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
                Debug.Log("[HeroModularVisualSetup] Hero prefab wired to the modular model + locomotion view. Use 'Place Moving Hero In Scene', then press Play.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
