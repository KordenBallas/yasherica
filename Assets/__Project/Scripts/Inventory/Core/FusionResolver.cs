using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// The cauldron's combine pipeline: match a rare authored signature first
    /// (RecipeBook, multiset-exact); otherwise compute an emergent target profile
    /// from the inputs' traits and select the authored artifact that best expresses
    /// it. Structurally total - the selector always returns a candidate while any
    /// artifact is authored, and a defensive identity fallback covers malformed
    /// data, so the no-failure rule can never regress at runtime.
    /// </summary>
    public class FusionResolver : IFusionResolver
    {
        private readonly IRecipeBook _recipeBook;
        private readonly IArtifactTraitSource _traitSource;
        private readonly EmergentFusionCalculator _calculator;
        private readonly ArtifactByTraitSelector _selector;
        private readonly TraitFusionRuleSet _rules;
        private readonly FusionSettings _settings;

        public FusionResolver(
            IRecipeBook recipeBook,
            IArtifactTraitSource traitSource,
            EmergentFusionCalculator calculator,
            ArtifactByTraitSelector selector,
            TraitFusionRuleSet rules,
            FusionSettings settings)
        {
            _recipeBook = recipeBook ?? throw new ArgumentNullException(nameof(recipeBook));
            _traitSource = traitSource ?? throw new ArgumentNullException(nameof(traitSource));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public FusionResult Resolve(IReadOnlyList<string> inputDefinitionIds)
        {
            if (inputDefinitionIds == null || inputDefinitionIds.Count == 0)
            {
                throw new ArgumentException("A combine needs at least one input.", nameof(inputDefinitionIds));
            }

            if (_recipeBook.TryMatch(inputDefinitionIds, out string signatureOutput))
            {
                return new FusionResult(signatureOutput, isSignature: true);
            }

            var profiles = new List<ArtifactTraitProfile>(inputDefinitionIds.Count);
            foreach (var id in inputDefinitionIds)
            {
                if (_traitSource.TryGetProfile(id, out var profile))
                {
                    profiles.Add(profile);
                }
            }

            var target = _calculator.ComputeTarget(profiles, _rules, _settings);
            string best = _selector.SelectBest(target, _traitSource, inputDefinitionIds, _settings);

            if (best == null)
            {
                // Defensive identity fallback: with an empty trait source (malformed
                // data) the combine still yields the last input back.
                best = inputDefinitionIds[inputDefinitionIds.Count - 1];
            }

            return new FusionResult(best, isSignature: false);
        }
    }
}
