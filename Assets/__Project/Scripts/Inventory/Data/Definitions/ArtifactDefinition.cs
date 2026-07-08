using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one artifact type.
    /// Contains ONLY configuration data - NO logic.
    /// Runtime artifacts are ArtifactInstance objects referencing this definition by Id.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtifactDefinition", menuName = "Inventory/Artifact")]
    public class ArtifactDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id referenced by recipes and save data (e.g. 'fire')")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [Tooltip("Tint of the bubble carrying this artifact inside the pot")]
        [SerializeField] private Color _bubbleTint = Color.white;

        [Header("Function Traits")]
        [Tooltip("What the artifact is made of (Substance-axis traits, e.g. stone, rot)")]
        [SerializeField] private TraitDefinition[] _substanceTraits;
        [Tooltip("What the artifact does (Property-axis traits, e.g. sharp, toxic)")]
        [SerializeField] private TraitDefinition[] _propertyTraits;
        [Tooltip("Potency: 0 = raw find, higher = crafted/refined")]
        [SerializeField, Min(0)] private int _tier;

        [Header("Reward Belonging (P1-5)")]
        [Tooltip("Coarse function family for the quest-reward economy (e.g. 'power' vs 'utility'); must match a RewardFamilyDefinition id. Empty = never rolled by a family-constrained quest reward.")]
        [SerializeField] private string _rewardFamilyId = string.Empty;

        [Header("Meta gating (Track R)")]
        [SerializeField] private MetaProgression.Data.MetaGatingAuthoring _metaGating = new MetaProgression.Data.MetaGatingAuthoring();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color BubbleTint => _bubbleTint;
        public IReadOnlyList<TraitDefinition> SubstanceTraits => _substanceTraits;
        public IReadOnlyList<TraitDefinition> PropertyTraits => _propertyTraits;
        public int Tier => _tier;

        /// <summary>Reward-family id (belonging) this artifact rolls under in the quest-reward
        /// economy; the family also supplies the offer card's belonging colour (P0-3·b).</summary>
        public string RewardFamilyId => _rewardFamilyId;

        /// <summary>Meta-progression gate (Track R): base vs meta-gated + deed. Unmarked = base.</summary>
        public MetaProgression.Data.MetaGatingAuthoring MetaGating =>
            _metaGating ?? (_metaGating = new MetaProgression.Data.MetaGatingAuthoring());
    }
}
