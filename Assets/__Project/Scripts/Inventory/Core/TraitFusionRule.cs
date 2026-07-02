using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// One authored step of the emergent fusion grammar in pure terms: when every
    /// required trait is present, the rule removes its consumed traits, adds its
    /// produced traits (transmute), and shifts the tier. A rule with no removals is
    /// a plain combine; the grammar's "amplify" (duplicate trait across inputs)
    /// is built into the calculator, not authored per rule.
    /// </summary>
    public class TraitFusionRule
    {
        public string RuleId { get; }
        public IReadOnlyList<string> RequiredTraitIds { get; }
        public IReadOnlyList<string> AddedTraitIds { get; }
        public IReadOnlyList<string> RemovedTraitIds { get; }
        public int TierDelta { get; }

        public TraitFusionRule(
            string ruleId,
            IEnumerable<string> requiredTraitIds,
            IEnumerable<string> addedTraitIds,
            IEnumerable<string> removedTraitIds,
            int tierDelta)
        {
            if (string.IsNullOrEmpty(ruleId))
            {
                throw new ArgumentException("Fusion rule id must be non-empty.", nameof(ruleId));
            }

            RuleId = ruleId;
            RequiredTraitIds = Normalize(requiredTraitIds);
            AddedTraitIds = Normalize(addedTraitIds);
            RemovedTraitIds = Normalize(removedTraitIds);
            TierDelta = tierDelta;

            if (RequiredTraitIds.Count == 0)
            {
                throw new ArgumentException(
                    $"Fusion rule '{ruleId}' must require at least one trait.", nameof(requiredTraitIds));
            }
        }

        private static IReadOnlyList<string> Normalize(IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            var result = new List<string>();

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    {
                        continue;
                    }

                    result.Add(id);
                }
            }

            return result;
        }
    }
}
