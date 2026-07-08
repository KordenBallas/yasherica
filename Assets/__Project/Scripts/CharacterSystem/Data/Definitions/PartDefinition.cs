using System.Collections.Generic;
using Combat.Data.Definitions;
using UnityEngine;
using World.Races.Data;

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
    /// in the ROADMAP.
    ///
    /// A part also carries its mutation data: a per-trait affinity vector, a rarity tier, and the
    /// icon shown on the unseal variant card. The variant scoring ranks every part of a blank's
    /// slot against the socketed reagents from this data (see mutation-subsystem.md §2.6). Hosting
    /// Mutation-layer concepts (trait affinity, rarity) here is a deliberate, user-approved
    /// decision (M2) for single-asset authoring; the layering trade-off is tracked in the ROADMAP,
    /// mirroring the accepted Combat coupling above. Affinity/rarity reference no Mutation type
    /// (id string + plain enum), so the mutation Core stays decoupled from this layer.
    /// </summary>
    [CreateAssetMenu(fileName = "PartDefinition", menuName = "Character System/Part")]
    public class PartDefinition : ScriptableObject
    {
        [SerializeField] private string _id;

        [Tooltip("Friendly name shown in UI (e.g. the mutation choice button). Falls back to the asset name when empty.")]
        [SerializeField] private string _displayName;

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

        [Header("Mutation (part-driven affinity)")]
        [Tooltip("Per-trait affinity (0..1) used to score this part as an unseal variant against the socketed reagents' traits (Socketed Blanks).")]
        [SerializeField] private List<TraitAffinity> _traitAffinities;

        [Tooltip("Rarity tier; rarer parts are favoured only once the socketed potency is high enough.")]
        [SerializeField] private MutationRarity _rarity = MutationRarity.Common;

        [Tooltip("Icon shown on the mutation choice button when this part is offered.")]
        [SerializeField] private Sprite _choiceIcon;

        [Header("Body plan")]
        [Tooltip("Rare frame-changing parts only: while equipped, this part is a candidate to govern the whole body plan — its Target Skeleton becomes the body's frame when it wins the priority resolution (body-plan-skeleton-swap.md).")]
        [SerializeField] private bool _governsBodyPlan;

        [Tooltip("Priority among equipped frame-changing parts: higher wins; ties break by ordinal part id. Ignored unless Governs Body Plan is set.")]
        [SerializeField] private int _bodyPlanPriority;

        [Header("Race (passport marker)")]
        [Tooltip("Race this part reads as for the passport (races-passport.md); empty = kindless. Any tagged part counts toward that race's acceptance tier.")]
        [RaceId]
        [SerializeField] private string _raceId;

        [Header("Meta gating (Track R)")]
        [SerializeField] private MetaProgression.Data.MetaGatingAuthoring _metaGating = new MetaProgression.Data.MetaGatingAuthoring();

        public string Id => _id;
        public string DisplayName => _displayName;
        public SlotDefinition Slot => _slot;
        public SkeletonDefinition TargetSkeleton => _targetSkeleton;
        public GameObject PartPrefab => _partPrefab;
        public IReadOnlyList<string> BoneNames => _boneNames ?? (IReadOnlyList<string>)System.Array.Empty<string>();
        public IReadOnlyList<SocketDefinition> ContributedSockets => _contributedSockets ?? (IReadOnlyList<SocketDefinition>)System.Array.Empty<SocketDefinition>();
        public IReadOnlyList<AbilityDefinition> ActiveAbilities => _activeAbilities ?? (IReadOnlyList<AbilityDefinition>)System.Array.Empty<AbilityDefinition>();
        public IReadOnlyList<PassiveAbilityDefinition> PassiveAbilities => _passiveAbilities ?? (IReadOnlyList<PassiveAbilityDefinition>)System.Array.Empty<PassiveAbilityDefinition>();
        public IReadOnlyList<TraitAffinity> TraitAffinities => _traitAffinities ?? (IReadOnlyList<TraitAffinity>)System.Array.Empty<TraitAffinity>();
        public MutationRarity Rarity => _rarity;
        public Sprite ChoiceIcon => _choiceIcon;

        /// <summary>True for the rare frame-changing parts that pull in their own skeleton
        /// (their <see cref="TargetSkeleton"/> governs the body when they win the resolution).</summary>
        public bool GovernsBodyPlan => _governsBodyPlan;

        /// <summary>Higher wins among equipped frame-changers; ties break by ordinal part id.</summary>
        public int BodyPlanPriority => _bodyPlanPriority;

        /// <summary>Race id this part is tagged to (passport marker); empty = kindless. An id string
        /// rather than a RaceDefinition reference, mirroring the M2 id-string decoupling; the
        /// [RaceId] attribute only drives the inspector drop-down.</summary>
        public string RaceId => _raceId;

        /// <summary>Meta-progression gate (Track R): base vs meta-gated + deed. Unmarked = base.</summary>
        public MetaProgression.Data.MetaGatingAuthoring MetaGating =>
            _metaGating ?? (_metaGating = new MetaProgression.Data.MetaGatingAuthoring());
    }
}
