using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Computes the target trait profile of an emergent (non-signature) fusion:
    /// union the input traits, apply the authored rules in their deterministic
    /// order (each at most once, against the evolving trait set), and derive the
    /// tier from the strongest input plus rule deltas plus the amplify bonus for
    /// every trait duplicated across inputs. Pure and allocation-light; the same
    /// grammar is reused by the operating table's socket interaction.
    /// </summary>
    public class EmergentFusionCalculator
    {
        public ArtifactTraitProfile ComputeTarget(
            IReadOnlyList<ArtifactTraitProfile> inputs,
            TraitFusionRuleSet rules,
            FusionSettings settings)
        {
            if (inputs == null || inputs.Count == 0)
            {
                return ArtifactTraitProfile.Empty;
            }

            var occurrences = new Dictionary<string, int>();
            int baseTier = 0;

            foreach (var input in inputs)
            {
                if (input == null)
                {
                    continue;
                }

                if (input.Tier > baseTier)
                {
                    baseTier = input.Tier;
                }

                foreach (var trait in input.Traits)
                {
                    occurrences.TryGetValue(trait, out int count);
                    occurrences[trait] = count + 1;
                }
            }

            var traits = new HashSet<string>(occurrences.Keys);
            int amplifiedTraits = 0;
            foreach (var pair in occurrences)
            {
                if (pair.Value >= 2)
                {
                    amplifiedTraits++;
                }
            }

            int tierDelta = ApplyRules(traits, rules);

            int tier = baseTier + tierDelta + amplifiedTraits * settings.AmplifyTierBonus;
            return ArtifactTraitProfile.Create(traits, tier);
        }

        private static int ApplyRules(HashSet<string> traits, TraitFusionRuleSet rules)
        {
            int tierDelta = 0;
            if (rules == null)
            {
                return tierDelta;
            }

            foreach (var rule in rules.Rules)
            {
                if (!AllPresent(traits, rule.RequiredTraitIds))
                {
                    continue;
                }

                foreach (var removed in rule.RemovedTraitIds)
                {
                    traits.Remove(removed);
                }

                foreach (var added in rule.AddedTraitIds)
                {
                    traits.Add(added);
                }

                tierDelta += rule.TierDelta;
            }

            return tierDelta;
        }

        private static bool AllPresent(HashSet<string> traits, IReadOnlyList<string> required)
        {
            for (int i = 0; i < required.Count; i++)
            {
                if (!traits.Contains(required[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
