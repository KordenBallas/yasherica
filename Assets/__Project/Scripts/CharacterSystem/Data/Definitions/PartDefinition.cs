using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A swappable body part: the slot it fills, the skeleton it is skinned for,
    /// its prefab (one SkinnedMeshRenderer), the bone names its mesh is bound to,
    /// and the Tier-2 sockets it contributes while equipped.
    /// </summary>
    [CreateAssetMenu(fileName = "PartDefinition", menuName = "Character System/Part")]
    public class PartDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private SlotDefinition _slot;
        [SerializeField] private SkeletonDefinition _targetSkeleton;
        [SerializeField] private GameObject _partPrefab;

        [Tooltip("Ordered to match the mesh's bone indices/bindposes. Use the inspector's 'Bake Bone Names From Prefab' button; never reorder by hand.")]
        [SerializeField] private List<string> _boneNames;

        [SerializeField] private List<SocketDefinition> _contributedSockets;

        public string Id => _id;
        public SlotDefinition Slot => _slot;
        public SkeletonDefinition TargetSkeleton => _targetSkeleton;
        public GameObject PartPrefab => _partPrefab;
        public IReadOnlyList<string> BoneNames => _boneNames ?? (IReadOnlyList<string>)System.Array.Empty<string>();
        public IReadOnlyList<SocketDefinition> ContributedSockets => _contributedSockets ?? (IReadOnlyList<SocketDefinition>)System.Array.Empty<SocketDefinition>();
    }
}
