using System.Linq;
using Character.Locomotion;
using Core.DI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Registers the locomotion subsystem on the open scene's SceneContext: ensures the
    /// CharacterSystem + CharacterLocomotion installers and a LocomotionConfig asset. It does NOT
    /// create, delete, or move any Hero — the scene's existing Hero.prefab instance (already the
    /// cameras' TrackingTarget and AreaSceneEntrypoint.characterTransform) is the player, and its
    /// CharacterLocomotionView is inherited from the prefab via 'Attach Modular Visual To Hero'.
    /// </summary>
    public static class SceneLocomotionSetup
    {
        private const string RootFolder = "Assets/__Project/Resources/CharacterSystem";
        private const string LocomotionConfigPath = RootFolder + "/LocomotionConfig.asset";

        [MenuItem("Tools/Character System/Setup Locomotion In Open Scene")]
        public static void SetupLocomotion()
        {
            var config = EnsureLocomotionConfig();

            EnsureInstaller<CharacterSystemInstaller>();
            var locomotionInstaller = EnsureInstaller<CharacterLocomotionInstaller>();
            if (locomotionInstaller == null)
            {
                return;
            }

            if (config != null)
            {
                AssignObjectField(locomotionInstaller, "_config", config);
            }

            EditorSceneManager.MarkSceneDirty(locomotionInstaller.gameObject.scene);
            Debug.Log("[SceneLocomotionSetup] CharacterLocomotionInstaller ensured on the SceneContext. " +
                      "Run 'Attach Modular Visual To Hero' so the Hero prefab carries the model + locomotion view, then press Play.");
        }

        private static LocomotionConfig EnsureLocomotionConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LocomotionConfig>(LocomotionConfigPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                Debug.LogWarning($"[SceneLocomotionSetup] {RootFolder} missing; generate placeholder assets first.");
                return null;
            }

            var config = ScriptableObject.CreateInstance<LocomotionConfig>();
            AssetDatabase.CreateAsset(config, LocomotionConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static TInstaller EnsureInstaller<TInstaller>() where TInstaller : MonoInstaller
        {
            var sceneContext = Object.FindObjectOfType<SceneContext>();
            if (sceneContext == null)
            {
                Debug.LogWarning("[SceneLocomotionSetup] No SceneContext found; add a Zenject SceneContext and the installers manually.");
                return null;
            }

            foreach (var existing in sceneContext.Installers)
            {
                if (existing is TInstaller installerComponent)
                {
                    return installerComponent;
                }
            }

            var installer = sceneContext.gameObject.GetComponent<TInstaller>();
            if (installer == null)
            {
                installer = sceneContext.gameObject.AddComponent<TInstaller>();
            }

            // Register through the public Installers property (its setter writes the serialized
            // _monoInstallers list). FindProperty("_installers") is wrong: the field was renamed to
            // _monoInstallers with [FormerlySerializedAs("_installers")], which FindProperty ignores.
            if (!sceneContext.Installers.Contains(installer))
            {
                var installers = sceneContext.Installers.ToList();
                installers.Add(installer);
                sceneContext.Installers = installers;
            }

            EditorUtility.SetDirty(sceneContext);
            return installer;
        }

        private static void AssignObjectField(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
