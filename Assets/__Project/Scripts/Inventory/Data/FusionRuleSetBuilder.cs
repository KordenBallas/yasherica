using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
using Inventory.Data.Definitions;

namespace Inventory.Data
{
    /// <summary>
    /// Converts authored FusionRuleDefinition assets into the pure-C# rule set.
    /// Malformed rules (missing id, no required traits, broken trait references)
    /// are skipped with a warning instead of breaking startup.
    /// </summary>
    public class FusionRuleSetBuilder
    {
        private readonly IGameLogger _logger;

        public FusionRuleSetBuilder(IGameLogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public TraitFusionRuleSet Build(IReadOnlyList<FusionRuleDefinition> definitions)
        {
            var rules = new List<TraitFusionRule>();

            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (TryConvert(definition, out var rule))
                    {
                        rules.Add(rule);
                    }
                }
            }

            return new TraitFusionRuleSet(rules);
        }

        private bool TryConvert(FusionRuleDefinition definition, out TraitFusionRule rule)
        {
            rule = null;

            if (definition == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(definition.RuleId))
            {
                _logger.Warning(LogCategory.Inventory,
                    $"[FusionRuleSetBuilder] Rule '{definition.name}' skipped: empty rule id.");
                return false;
            }

            if (!TryCollectIds(definition.RequiredTraits, definition, "required", out var required) ||
                required.Count == 0)
            {
                _logger.Warning(LogCategory.Inventory,
                    $"[FusionRuleSetBuilder] Rule '{definition.RuleId}' skipped: no valid required traits.");
                return false;
            }

            if (!TryCollectIds(definition.AddedTraits, definition, "added", out var added) ||
                !TryCollectIds(definition.RemovedTraits, definition, "removed", out var removed))
            {
                return false;
            }

            rule = new TraitFusionRule(definition.RuleId, required, added, removed, definition.TierDelta);
            return true;
        }

        private bool TryCollectIds(
            IReadOnlyList<TraitDefinition> traits,
            FusionRuleDefinition definition,
            string listName,
            out List<string> ids)
        {
            ids = new List<string>();

            if (traits == null)
            {
                return true;
            }

            foreach (var trait in traits)
            {
                if (trait == null || string.IsNullOrEmpty(trait.Id))
                {
                    _logger.Warning(LogCategory.Inventory,
                        $"[FusionRuleSetBuilder] Rule '{definition.RuleId}' skipped: broken {listName} trait reference.");
                    return false;
                }

                ids.Add(trait.Id);
            }

            return true;
        }
    }
}
