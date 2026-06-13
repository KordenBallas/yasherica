using System.Collections.Generic;
using CharacterSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterSystem
{
    /// <summary>
    /// Builds the placeholder skeleton bone hierarchy programmatically. The rig is
    /// created at the origin with identity rotations so every bone's worldToLocalMatrix
    /// can serve directly as the mesh bindpose.
    /// </summary>
    public static class PlaceholderRigBuilder
    {
        public const string RootBoneName = "Root";

        /// <summary>Bone name -> (parent bone name, local position). Order defines creation order.</summary>
        public static readonly (string Name, string Parent, Vector3 LocalPosition)[] Bones =
        {
            (RootBoneName, null, Vector3.zero),
            ("Pelvis", "Root", new Vector3(0f, 0.8f, 0f)),
            ("Spine", "Pelvis", new Vector3(0f, 0.15f, 0f)),
            ("Chest", "Spine", new Vector3(0f, 0.2f, 0f)),
            ("Neck", "Chest", new Vector3(0f, 0.15f, 0f)),
            ("Head", "Neck", new Vector3(0f, 0.1f, 0f)),
            ("Ear.L", "Head", new Vector3(-0.08f, 0.12f, 0f)),
            ("Ear.R", "Head", new Vector3(0.08f, 0.12f, 0f)),
            ("Shoulder.L", "Chest", new Vector3(-0.12f, 0.12f, 0f)),
            ("UpperArm.L", "Shoulder.L", new Vector3(-0.1f, 0f, 0f)),
            ("LowerArm.L", "UpperArm.L", new Vector3(-0.18f, 0f, 0f)),
            ("Hand.L", "LowerArm.L", new Vector3(-0.16f, 0f, 0f)),
            ("Shoulder.R", "Chest", new Vector3(0.12f, 0.12f, 0f)),
            ("UpperArm.R", "Shoulder.R", new Vector3(0.1f, 0f, 0f)),
            ("LowerArm.R", "UpperArm.R", new Vector3(0.18f, 0f, 0f)),
            ("Hand.R", "LowerArm.R", new Vector3(0.16f, 0f, 0f)),
            ("Wing.L", "Chest", new Vector3(-0.1f, 0.05f, 0.08f)),
            ("Wing.R", "Chest", new Vector3(0.1f, 0.05f, 0.08f)),
            ("Tail.0", "Pelvis", new Vector3(0f, -0.05f, 0.1f)),
            ("Tail.1", "Tail.0", new Vector3(0f, -0.02f, 0.15f)),
            ("Tail.2", "Tail.1", new Vector3(0f, 0f, 0.15f)),
            ("UpperLeg.L", "Pelvis", new Vector3(-0.1f, -0.05f, 0f)),
            ("LowerLeg.L", "UpperLeg.L", new Vector3(0f, -0.35f, 0f)),
            ("Foot.L", "LowerLeg.L", new Vector3(0f, -0.35f, 0f)),
            ("UpperLeg.R", "Pelvis", new Vector3(0.1f, -0.05f, 0f)),
            ("LowerLeg.R", "UpperLeg.R", new Vector3(0f, -0.35f, 0f)),
            ("Foot.R", "LowerLeg.R", new Vector3(0f, -0.35f, 0f))
        };

        /// <summary>
        /// Creates the rig GameObject in the open scene (caller is responsible for saving
        /// it as a prefab and destroying the scene instance). Returns the rig root holding
        /// Animator + CharacterRig, with the bone hierarchy underneath.
        /// </summary>
        public static GameObject BuildRigInScene(string rigName, out Dictionary<string, Transform> bonesByName)
        {
            var rigRoot = new GameObject(rigName);
            bonesByName = new Dictionary<string, Transform>(System.StringComparer.Ordinal);

            foreach (var (name, parent, localPosition) in Bones)
            {
                var bone = new GameObject(name).transform;
                bone.SetParent(parent == null ? rigRoot.transform : bonesByName[parent], false);
                bone.localPosition = localPosition;
                bonesByName.Add(name, bone);
            }

            var animator = rigRoot.AddComponent<Animator>();
            var rig = rigRoot.AddComponent<CharacterRig>();

            var serializedRig = new SerializedObject(rig);
            serializedRig.FindProperty("_rootBone").objectReferenceValue = bonesByName[RootBoneName];
            serializedRig.FindProperty("_animator").objectReferenceValue = animator;
            serializedRig.ApplyModifiedPropertiesWithoutUndo();

            return rigRoot;
        }
    }
}
