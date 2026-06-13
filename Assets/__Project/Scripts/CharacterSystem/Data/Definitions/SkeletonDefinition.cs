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
        [SerializeField] private GameObject _rigPrefab;

        [Tooltip("Authoritative bone-name list. Use the inspector's 'Sync Bone Names From Rig Prefab' button.")]
        [SerializeField] private List<string> _boneNames;

        [SerializeField] private List<SocketDefinition> _tier1Sockets;

        public string Id => _id;
        public GameObject RigPrefab => _rigPrefab;
        public IReadOnlyList<string> BoneNames => _boneNames ?? (IReadOnlyList<string>)System.Array.Empty<string>();
        public IReadOnlyList<SocketDefinition> Tier1Sockets => _tier1Sockets ?? (IReadOnlyList<SocketDefinition>)System.Array.Empty<SocketDefinition>();
    }
}
