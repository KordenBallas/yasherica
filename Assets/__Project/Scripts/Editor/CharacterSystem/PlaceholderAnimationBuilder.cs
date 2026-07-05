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

        /// <summary>Base-frame idle (kept for LocomotionControllerRebuilder; the generator goes
        /// through <see cref="BuildSwayClip"/> with the frame library's sway tables).</summary>
        public static AnimationClip BuildIdleClip(string assetPath)
        {
            return BuildSwayClip("PlaceholderIdle", assetPath, PlaceholderFrameLibrary.Base.IdleSways, PlaceholderFrameLibrary.Base.BonePath);
        }

        /// <summary>Base-frame run. The gait's fore/aft sign lives in the frame library's sway
        /// table (-1 reads forward for the -Z-facing placeholder model).</summary>
        public static AnimationClip BuildRunClip(string assetPath)
        {
            return BuildSwayClip("PlaceholderRun", assetPath, PlaceholderFrameLibrary.Base.RunSways, PlaceholderFrameLibrary.Base.BonePath);
        }

        /// <summary>
        /// Builds a looping clip from a frame's sway table: each entry becomes a sampled sine
        /// oscillation on the bone resolved to its full path by <paramref name="bonePath"/>.
        /// Frequencies must divide evenly into the clip length for a seamless loop.
        /// </summary>
        public static AnimationClip BuildSwayClip(
            string clipName,
            string assetPath,
            System.Collections.Generic.IReadOnlyList<PlaceholderFrameLibrary.SwaySpec> sways,
            System.Func<string, string> bonePath)
        {
            var clip = new AnimationClip { name = clipName };
            foreach (var sway in sways)
            {
                AddSwayCurve(
                    clip,
                    bonePath(sway.Bone),
                    sway.Axis,
                    sway.AmplitudeDegrees,
                    sway.FrequencyHz,
                    sway.PhaseFraction,
                    sway.CenterOffsetDegrees);
            }

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
