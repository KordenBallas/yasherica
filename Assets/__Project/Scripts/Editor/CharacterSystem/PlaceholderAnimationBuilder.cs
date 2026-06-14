using Character.Locomotion;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Authors the placeholder locomotion clips in code: quaternion localRotation curves
    /// (sampled sine oscillations) so that swap-without-interruption and appendage deformation
    /// are visually verifiable. The idle clip sways the upper body; the run clip drives a leg
    /// gait with counter-swinging arms. Both feed a 1D blend tree controller on a "Speed" param.
    /// </summary>
    public static class PlaceholderAnimationBuilder
    {
        private const float ClipLength = 2f;
        private const int SamplesPerSecond = 30;

        public static AnimationClip BuildIdleClip(string assetPath)
        {
            var clip = new AnimationClip { name = "PlaceholderIdle" };

            AddSwayCurve(clip, "Root/Pelvis/Spine", Vector3.forward, 5f, 0.5f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Neck/Head", Vector3.right, 6f, 0.5f, 0.2f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Neck/Head/Ear.L", Vector3.right, 12f, 1.5f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Neck/Head/Ear.R", Vector3.right, 12f, 1.5f, 0.5f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Shoulder.L/UpperArm.L", Vector3.right, 20f, 0.5f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Shoulder.R/UpperArm.R", Vector3.right, 20f, 0.5f, 0.5f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Wing.L", Vector3.forward, 25f, 1f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Wing.R", Vector3.forward, -25f, 1f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Tail.0", Vector3.up, 15f, 0.5f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Tail.0/Tail.1", Vector3.up, 15f, 0.5f, 0.15f);
            AddSwayCurve(clip, "Root/Pelvis/Tail.0/Tail.1/Tail.2", Vector3.up, 15f, 0.5f, 0.3f);

            return FinalizeLoopingClip(clip, assetPath);
        }

        public static AnimationClip BuildRunClip(string assetPath)
        {
            var clip = new AnimationClip { name = "PlaceholderRun" };

            // Two strides over the 2 s loop (frequency * ClipLength is integral, so it loops
            // seamlessly). Left/right limbs are half a cycle out of phase; arms counter-swing
            // the opposite leg; knees flex around a bent center; the spine bobs twice per stride.
            AddSwayCurve(clip, "Root/Pelvis/UpperLeg.L", Vector3.right, 35f, 1f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/UpperLeg.R", Vector3.right, 35f, 1f, 0.5f);
            AddSwayCurve(clip, "Root/Pelvis/UpperLeg.L/LowerLeg.L", Vector3.right, 30f, 1f, 0.25f, -30f);
            AddSwayCurve(clip, "Root/Pelvis/UpperLeg.R/LowerLeg.R", Vector3.right, 30f, 1f, 0.75f, -30f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Shoulder.L/UpperArm.L", Vector3.right, 30f, 1f, 0.5f);
            AddSwayCurve(clip, "Root/Pelvis/Spine/Chest/Shoulder.R/UpperArm.R", Vector3.right, 30f, 1f, 0f);
            AddSwayCurve(clip, "Root/Pelvis/Spine", Vector3.right, 5f, 2f, 0f);

            return FinalizeLoopingClip(clip, assetPath);
        }

        /// <summary>
        /// Builds a controller with a single "Speed" float driving a 1D blend tree:
        /// idle at threshold 0, run at threshold 1. The default parameter value (0) keeps a
        /// freshly-instantiated character idling until movement raises Speed.
        /// </summary>
        public static AnimatorController BuildController(AnimationClip idleClip, AnimationClip runClip, string assetPath)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            controller.AddParameter(LocomotionAnimatorParameters.Speed, AnimatorControllerParameterType.Float);

            var blendTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = LocomotionAnimatorParameters.Speed,
                useAutomaticThresholds = false
            };
            // The blend tree must live as a sub-asset of the controller to persist.
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(runClip, 1f);

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Locomotion");
            state.motion = blendTree;
            stateMachine.defaultState = state;

            return controller;
        }

        private static AnimationClip FinalizeLoopingClip(AnimationClip clip, string assetPath)
        {
            clip.EnsureQuaternionContinuity();

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        /// <summary>
        /// Adds a sampled sine oscillation around <paramref name="axis"/> as quaternion
        /// localRotation curves. Sampling (instead of Euler curves) avoids interpolation
        /// surprises and keeps the loop seamless: phase is in whole-cycle fractions and
        /// frequency divides evenly into the clip length. <paramref name="centerOffsetDegrees"/>
        /// shifts the oscillation center (e.g. a flexed knee bending around a bent pose).
        /// </summary>
        private static void AddSwayCurve(AnimationClip clip, string bonePath, Vector3 axis, float amplitudeDegrees, float frequencyHz, float phaseFraction, float centerOffsetDegrees = 0f)
        {
            var sampleCount = (int)(ClipLength * SamplesPerSecond) + 1;
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            var curveW = new AnimationCurve();

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)SamplesPerSecond;
                var angle = centerOffsetDegrees + amplitudeDegrees * Mathf.Sin(2f * Mathf.PI * (frequencyHz * time + phaseFraction));
                var rotation = Quaternion.AngleAxis(angle, axis);

                curveX.AddKey(time, rotation.x);
                curveY.AddKey(time, rotation.y);
                curveZ.AddKey(time, rotation.z);
                curveW.AddKey(time, rotation.w);
            }

            clip.SetCurve(bonePath, typeof(Transform), "localRotation.x", curveX);
            clip.SetCurve(bonePath, typeof(Transform), "localRotation.y", curveY);
            clip.SetCurve(bonePath, typeof(Transform), "localRotation.z", curveZ);
            clip.SetCurve(bonePath, typeof(Transform), "localRotation.w", curveW);
        }
    }
}
