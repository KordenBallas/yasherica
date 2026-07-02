using Core.Logging;
using Inventory.Core;
using Inventory.Data;
using Inventory.Data.Definitions;
using Zenject;

namespace Inventory.Application
{
    /// <summary>
    /// Startup check that surfaces trait-authoring mistakes: broken/misplaced trait
    /// references on artifacts, artifacts with no traits at all (they cannot steer
    /// the emergent fusion and break the "name and look telegraph traits" rule),
    /// and fusion rules that push toward traits no authored artifact carries (the
    /// selector could never express them). Logs warnings only - bad data is skipped
    /// downstream, never fatal. Runs after all installers so every catalog exists.
    /// </summary>
    public class ArtifactContentValidator : IInitializable
    {
        private readonly IArtifactCatalog _artifacts;
        private readonly ITraitCatalog _traits;
        private readonly IArtifactTraitSource _traitSource;
        private readonly TraitFusionRuleSet _fusionRules;
        private readonly IGameLogger _logger;

        public ArtifactContentValidator(
            IArtifactCatalog artifacts,
            ITraitCatalog traits,
            IArtifactTraitSource traitSource,
            TraitFusionRuleSet fusionRules,
            IGameLogger logger)
        {
            _artifacts = artifacts;
            _traits = traits;
            _traitSource = traitSource;
            _fusionRules = fusionRules;
            _logger = logger;
        }

        public void Initialize()
        {
            ValidateArtifactTraits();
            ValidateFusionRuleReachability();
        }

        private void ValidateArtifactTraits()
        {
            foreach (var artifact in _artifacts.All)
            {
                if (artifact == null)
                {
                    continue;
                }

                int traitCount = 0;
                traitCount += ValidateTraitList(artifact, artifact.SubstanceTraits, TraitAxis.Substance);
                traitCount += ValidateTraitList(artifact, artifact.PropertyTraits, TraitAxis.Property);

                if (traitCount == 0)
                {
                    _logger.Warning(LogCategory.Inventory,
                        $"[ArtifactContentValidator] Artifact '{artifact.Id}' has no traits; it cannot " +
                        "steer emergent fusion and its look cannot telegraph a function.");
                }
            }
        }

        private int ValidateTraitList(
            ArtifactDefinition artifact,
            System.Collections.Generic.IReadOnlyList<TraitDefinition> list,
            TraitAxis expectedAxis)
        {
            if (list == null)
            {
                return 0;
            }

            int valid = 0;
            foreach (var trait in list)
            {
                if (trait == null || string.IsNullOrEmpty(trait.Id))
                {
                    _logger.Warning(LogCategory.Inventory,
                        $"[ArtifactContentValidator] Artifact '{artifact.Id}' has a broken " +
                        $"{expectedAxis} trait reference; it will be ignored.");
                    continue;
                }

                if (!_traits.Contains(trait.Id))
                {
                    _logger.Warning(LogCategory.Inventory,
                        $"[ArtifactContentValidator] Artifact '{artifact.Id}' references trait " +
                        $"'{trait.Id}' that is not in the trait catalog (is the asset outside " +
                        "Resources/Artifacts/Traits?).");
                }

                if (trait.Axis != expectedAxis)
                {
                    _logger.Warning(LogCategory.Inventory,
                        $"[ArtifactContentValidator] Artifact '{artifact.Id}' lists trait '{trait.Id}' " +
                        $"({trait.Axis}) under its {expectedAxis} traits; the grammar still reads it, " +
                        "but move it to the matching list for authoring clarity.");
                }

                valid++;
            }

            return valid;
        }

        private void ValidateFusionRuleReachability()
        {
            foreach (var rule in _fusionRules.Rules)
            {
                foreach (var added in rule.AddedTraitIds)
                {
                    if (!_traits.Contains(added))
                    {
                        _logger.Warning(LogCategory.Inventory,
                            $"[ArtifactContentValidator] Fusion rule '{rule.RuleId}' adds unknown trait " +
                            $"'{added}'. Add a TraitDefinition with that id or fix the rule.");
                        continue;
                    }

                    if (!AnyArtifactCarries(added))
                    {
                        _logger.Warning(LogCategory.Inventory,
                            $"[ArtifactContentValidator] Fusion rule '{rule.RuleId}' pushes toward trait " +
                            $"'{added}' that no authored artifact carries - the fusion output can never " +
                            "express it. Author an artifact with that trait.");
                    }
                }
            }
        }

        private bool AnyArtifactCarries(string traitId)
        {
            foreach (var entry in _traitSource.All)
            {
                if (entry.Profile != null && entry.Profile.Has(traitId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
