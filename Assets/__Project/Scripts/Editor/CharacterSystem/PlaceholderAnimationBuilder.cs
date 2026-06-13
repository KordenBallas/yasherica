using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Authors the placeholder looping idle clip in code: quaternion localRotation curves
    /// (sampled sine oscillations) on spine, head, arms, ears, wings, and tail so that
    /// swap-without-interruption and appendage deformation are visually verifiable.
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

            clip.EnsureQuaternionContinuity();

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        public static AnimatorController BuildController(AnimationClip idleClip, string assetPath)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(assetPath, idleClip);
            return controller;
        }

        /// <summary>
        /// Adds a sampled sine oscillation around <paramref name="axis"/> as quaternion
        /// localRotation curves. Sampling (instead of Euler curves) avoids interpolation
        /// surprises and keeps the loop seamless: phase is in whole-cycle fractions and
        /// frequency divides evenly into the clip length.
        /// </summary>
        private static void AddSwayCurve(AnimationClip clip, string bonePath, Vector3 axis, float amplitudeDegrees, float frequencyHz, float phaseFraction)
        {
            var sampleCount = (int)(ClipLength * SamplesPerSecond) + 1;
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            var curveW = new AnimationCurve();

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)SamplesPerSecond;
                var angle = amplitudeDegrees * Mathf.Sin(2f * Mathf.PI * (frequencyHz * time + phaseFraction));
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
