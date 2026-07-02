using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// The authored fusion rules in the one deterministic order the grammar applies
    /// them: ordinal by rule id. Fails fast on duplicate ids so authoring mistakes
    /// surface at startup instead of silently shadowing a rule.
    /// </summary>
    public class TraitFusionRuleSet
    {
        public static readonly TraitFusionRuleSet Empty = new TraitFusionRuleSet(new List<TraitFusionRule>());

        private readonly List<TraitFusionRule> _rules;

        /// <summary>Rules in ordinal rule-id order.</summary>
        public IReadOnlyList<TraitFusionRule> Rules => _rules;

        public TraitFusionRuleSet(IReadOnlyList<TraitFusionRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var byId = new HashSet<string>();
            _rules = new List<TraitFusionRule>(rules.Count);

            foreach (var rule in rules)
            {
                if (rule == null)
                {
                    continue;
                }

                if (!byId.Add(rule.RuleId))
                {
                    throw new InvalidOperationException(
                        $"[TraitFusionRuleSet] Duplicate fusion rule id '{rule.RuleId}'.");
                }

                _rules.Add(rule);
            }

            _rules.Sort((a, b) => string.CompareOrdinal(a.RuleId, b.RuleId));
        }
    }
}
