using CharacterSystem.Data;
using Core.Logging;
using Inventory.Data;
using Mutation.Core;
using Mutation.Data;
using Zenject;

namespace Mutation.Application
{
    /// <summary>
    /// Startup check that surfaces mutation-authoring mistakes: any body part whose trait affinity
    /// names an empty or unknown trait id, and any Part-Blank with a broken slot, an unknown species
    /// archetype, or fewer than two candidate parts for its slot (an unseal menu of one is no
    /// choice). Logs warnings only - bad data is skipped downstream, never fatal. Runs after all
    /// installers so every catalog exists. (Artifact trait authoring is validated by the Inventory
    /// side's ArtifactContentValidator.)
    /// </summary>
    public class MutationContentValidator : IInitializable
    {
        private readonly IArchetypeCatalog _archetypes;
        private readonly IPartCatalog _parts;
        private readonly ITraitCatalog _traits;
        private readonly IPartBlankDataSource _blanks;
        private readonly IMutationPartCatalog _mutationParts;
        private readonly IGameLogger _logger;

        public MutationContentValidator(
            IArchetypeCatalog archetypes,
            IPartCatalog parts,
            ITraitCatalog traits,
            IPartBlankDataSource blanks,
            IMutationPartCatalog mutationParts,
            IGameLogger logger)
        {
            _archetypes = archetypes;
            _parts = parts;
            _traits = traits;
            _blanks = blanks;
            _mutationParts = mutationParts;
            _logger = logger;
        }

        public void Initialize()
        {
            ValidatePartTraitAffinities();
            ValidateBlanks();
        }

        private void ValidatePartTraitAffinities()
        {
            foreach (var part in _parts.All)
            {
                if (part == null)
                {
                    continue;
                }

                foreach (var affinity in part.TraitAffinities)
                {
                    if (affinity == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(affinity.TraitId))
                    {
                        _logger.Warning(LogCategory.Mutation,
                            $"[MutationContentValidator] Part '{part.Id}' has a trait affinity with an " +
                            "empty trait id; it will be ignored when scoring unseal variants.");
                        continue;
                    }

                    if (!_traits.Contains(affinity.TraitId))
                    {
                        _logger.Warning(LogCategory.Mutation,
                            $"[MutationContentValidator] Part '{part.Id}' has a trait affinity for " +
                            $"unknown trait id '{affinity.TraitId}'. Add a TraitDefinition with that id " +
                            "or fix the part.");
                    }
                }
            }
        }

        private void ValidateBlanks()
        {
            foreach (var blank in _blanks.All)
            {
                if (blank == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(blank.SlotId))
                {
                    _logger.Warning(LogCategory.Mutation,
                        $"[MutationContentValidator] Blank '{blank.DefinitionId}' has no slot reference; " +
                        "it can never unseal into a part.");
                }

                if (string.IsNullOrEmpty(blank.SpeciesArchetypeId) ||
                    !_archetypes.Contains(blank.SpeciesArchetypeId))
                {
                    _logger.Warning(LogCategory.Mutation,
                        $"[MutationContentValidator] Blank '{blank.DefinitionId}' has an empty or unknown " +
                        $"species archetype id '{blank.SpeciesArchetypeId}'. The passport marker and card " +
                        "tint need an ArchetypeDefinition.");
                }

                int candidates = CountCandidatesForSlot(blank.SlotId);
                if (candidates < 2)
                {
                    _logger.Warning(LogCategory.Mutation,
                        $"[MutationContentValidator] Blank '{blank.DefinitionId}' has {candidates} candidate " +
                        $"part(s) for slot '{blank.SlotId}'; an unseal menu needs at least 2 authored parts " +
                        "to be a choice (equipped parts are excluded at unseal time).");
                }
            }
        }

        private int CountCandidatesForSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return 0;
            }

            int count = 0;
            foreach (var candidate in _mutationParts.AllCandidates)
            {
                if (candidate != null && string.Equals(candidate.SlotId, slotId, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
