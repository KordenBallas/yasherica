using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Read access to one instantiated skeleton: its bones by name,
    /// the renderer root bone, and the single Animator.
    /// </summary>
    public interface ICharacterRig
    {
        /// <summary>The rig prefab root (holds the Animator); part instances parent here.</summary>
        Transform Root { get; }

        /// <summary>The top bone of the skeleton, assigned to SkinnedMeshRenderer.rootBone.</summary>
        Transform RootBone { get; }

        Animator Animator { get; }

        bool TryGetBone(string boneName, out Transform bone);
    }
}
