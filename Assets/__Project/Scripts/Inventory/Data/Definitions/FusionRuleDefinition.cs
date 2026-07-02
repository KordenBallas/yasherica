using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one authored step of the emergent fusion
    /// grammar (combine / transmute). Contains ONLY configuration data - NO logic.
    /// When every required trait is present in a combine, the rule removes its
    /// consumed traits, adds its produced traits, and shifts the tier. Rules apply
    /// in ordinal rule-id order; a designer adds one by creating an asset.
    /// </summary>
    [CreateAssetMenu(fileName = "FusionRuleDefinition", menuName = "Inventory/Fusion Rule")]
    public class FusionRuleDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id; also the deterministic application order (ordinal)")]
        [SerializeField] private string _ruleId;

        [Header("Grammar")]
        [Tooltip("All of these traits must be present for the rule to fire")]
        [SerializeField] private List<TraitDefinition> _requiredTraits;
        [Tooltip("Traits the rule adds to the result (the emergent property)")]
        [SerializeField] private List<TraitDefinition> _addedTraits;
        [Tooltip("Traits the rule consumes (transmute); empty = plain combine")]
        [SerializeField] private List<TraitDefinition> _removedTraits;
        [Tooltip("Tier shift the rule applies to the result")]
        [SerializeField] private int _tierDelta;

        public string RuleId => _ruleId;
        public IReadOnlyList<TraitDefinition> RequiredTraits => _requiredTraits;
        public IReadOnlyList<TraitDefinition> AddedTraits => _addedTraits;
        public IReadOnlyList<TraitDefinition> RemovedTraits => _removedTraits;
        public int TierDelta => _tierDelta;
    }
}
