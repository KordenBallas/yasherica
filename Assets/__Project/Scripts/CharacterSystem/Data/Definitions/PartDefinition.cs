using System.Collections.Generic;
using Combat.Data.Definitions;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// A swappable body part: the slot it fills, the skeleton it is skinned for,
    /// its prefab (one SkinnedMeshRenderer), the bone names its mesh is bound to,
    /// and the Tier-2 sockets it contributes while equipped.
    ///
    /// A part also declares the combat abilities equipping it grants: active abilities
    /// (usable in combat) and passive abilities (standing modifiers applied for the whole
    /// combat). NOTE: referencing the Combat data layer from CharacterSystem is a deliberate,
    /// user-approved coupling (M1) that violates CLAUDE.md §2 layering; it is tracked as debt
    /// in the ROADMAP and is expected to be revisited by the M2 part-driven-affinity item.
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

        [Header("Combat (granted while equipped)")]
        [Tooltip("Active abilities this part makes available in combat.")]
        [SerializeField] private List<AbilityDefinition> _activeAbilities;

        [Tooltip("Passive (always-on) abilities applied as standing modifiers for the whole combat.")]
        [SerializeField] private List<PassiveAbilityDefinition> _passiveAbilities;

        public string Id => _id;
        public SlotDefinition Slot => _slot;
        public SkeletonDefinition TargetSkeleton => _targetSkeleton;
        public GameObject PartPrefab => _partPrefab;
        public IReadOnlyList<string> BoneNames => _boneNames ?? (IReadOnlyList<string>)System.Array.Empty<string>();
        public IReadOnlyList<SocketDefinition> ContributedSockets => _contributedSockets ?? (IReadOnlyList<SocketDefinition>)System.Array.Empty<SocketDefinition>();
        public IReadOnlyList<AbilityDefinition> ActiveAbilities => _activeAbilities ?? (IReadOnlyList<AbilityDefinition>)System.Array.Empty<AbilityDefinition>();
        public IReadOnlyList<PassiveAbilityDefinition> PassiveAbilities => _passiveAbilities ?? (IReadOnlyList<PassiveAbilityDefinition>)System.Array.Empty<PassiveAbilityDefinition>();
    }
}
