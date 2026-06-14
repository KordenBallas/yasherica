using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Re-adds the run blend to the placeholder rig WITHOUT rebuilding the rig's structure: it builds
    /// PlaceholderRun.anim + a 1D blend-tree controller (idle + run on a "Speed" float) and assigns the
    /// controller onto the existing PlaceholderRig.prefab through a LoadPrefabContents/SaveAsPrefabAsset
    /// round-trip. That preserves the rig's root-GameObject fileID, which SkeletonDefinition._rigPrefab
    /// references — so a full asset regeneration (which would churn those fileIDs and break the
    /// skeleton link) is not needed just to swap the animator controller.
    /// </summary>
    public static class LocomotionControllerRebuilder
    {
        private const string AnimFolder = "Assets/__Project/Resources/CharacterSystem/Animation";
        private const string IdleClipPath = AnimFolder + "/PlaceholderIdle.anim";
        private const string RunClipPath = AnimFolder + "/PlaceholderRun.anim";
        private const string ControllerPath = AnimFolder + "/PlaceholderLocomotion.controller";
        private const string RigPrefabPath = "Assets/__Project/Resources/CharacterSystem/Skeletons/PlaceholderRig.prefab";

        [MenuItem("Tools/Character System/Rebuild Locomotion Controller")]
        public static void Rebuild()
        {
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (idleClip == null)
            {
                Debug.LogError($"[LocomotionControllerRebuilder] Idle clip not found at {IdleClipPath}. Generate/restore the placeholder assets first.");
                return;
            }

            // Rebuild the run clip + blend-tree controller from scratch (idempotent).
            AssetDatabase.DeleteAsset(RunClipPath);
            AssetDatabase.DeleteAsset(ControllerPath);
            var runClip = PlaceholderAnimationBuilder.BuildRunClip(RunClipPath);
            var controller = PlaceholderAnimationBuilder.BuildController(idleClip, runClip, ControllerPath);

            var rig = PrefabUtility.LoadPrefabContents(RigPrefabPath);
            if (rig == null)
            {
                Debug.LogError($"[LocomotionControllerRebuilder] Could not load rig prefab at {RigPrefabPath}.");
                return;
            }

            try
            {
                var animator = rig.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError("[LocomotionControllerRebuilder] Rig prefab root has no Animator.");
                    return;
                }

                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(rig, RigPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rig);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[LocomotionControllerRebuilder] Rebuilt PlaceholderRun.anim + PlaceholderLocomotion.controller (1D blend on Speed) and assigned it to PlaceholderRig.prefab.");
        }
    }
}
