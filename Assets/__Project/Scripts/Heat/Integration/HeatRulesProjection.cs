using Combat.Core;
using Heat.Core;
using LevelGeneration.Journey;
using Mutation.Core;

namespace Heat.Integration
{
    /// <summary>
    /// Projects the composed <see cref="HeatRules"/> onto the per-system rule records — the one
    /// place that knows which Heat effect lands in which system, so no consuming system ever
    /// references <c>Heat.*</c> and the installer mappings stay one-liners.
    /// </summary>
    public static class HeatRulesProjection
    {
        public static CombatRuleModifiers ToCombat(HeatRules rules)
        {
            return rules == null || !rules.EnemiesAlwaysLead
                ? CombatRuleModifiers.Neutral
                : new CombatRuleModifiers(enemiesAlwaysLead: true);
        }

        public static JourneyRuleModifiers ToJourney(HeatRules rules)
        {
            return rules == null || rules.EscalationTierLift == 0
                ? JourneyRuleModifiers.Neutral
                : new JourneyRuleModifiers(rules.EscalationTierLift);
        }

        public static MutationRuleModifiers ToMutation(HeatRules rules)
        {
            return rules == null || (rules.VariantOptionCut == 0 && rules.SocketCut == 0)
                ? MutationRuleModifiers.Neutral
                : new MutationRuleModifiers(rules.VariantOptionCut, rules.SocketCut);
        }
    }
}
