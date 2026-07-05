using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// One shared skeleton: the rig prefab (full bone hierarchy + Animator + CharacterRig),
    /// the authoritative bone-name list, and the Tier-1 sockets that always exist on it.
    /// </summary>
    [CreateAssetMenu(fileName = "SkeletonDefinition", menuName = "Character System/Skeleton")]
    public class SkeletonDefinition : ScriptableObject
    {
        [SerializeField] private string _id;

        [Tooltip("Friendly name shown in UI (e.g. the body-plan confirm dialog). Falls back to the id when empty.")]
        [SerializeField] private string _displayName;

        [SerializeField] private GameObject _rigPrefab;

        [Tooltip("Authoritative bone-name list. Use the inspector's 'Sync Bone Names From Rig Prefab' button.")]
        [SerializeField] private List<string> _boneNames;

        [SerializeField] private List<SocketDefinition> _tier1Sockets;

        [Tooltip("Locomotion controller bound when this skeleton governs the body (per-frame gait). Falls back to the visual's serialized controller, then the placeholder, when empty.")]
        [SerializeField] private RuntimeAnimatorController _animatorController;

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;
        public GameObject RigPrefab => _rigPrefab;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public IReadOnlyList<string> BoneNames => _boneNames ?? (IReadOnlyList<string>)System.Array.Empty<string>();
        public IReadOnlyList<SocketDefinition> Tier1Sockets => _tier1Sockets ?? (IReadOnlyList<SocketDefinition>)System.Array.Empty<SocketDefinition>();
    }
}
