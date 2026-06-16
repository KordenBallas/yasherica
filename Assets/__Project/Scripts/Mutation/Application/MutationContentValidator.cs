using System.Collections.Generic;
using CharacterSystem.Data;
using Core.Logging;
using Inventory.Data;
using Mutation.Data;
using Mutation.Data.Definitions;
using Zenject;

namespace Mutation.Application
{
    /// <summary>
    /// Startup check that surfaces authoring mistakes: any artifact whose archetype weights
    /// reference an unknown archetype id, and any stage-up part-set option that references an
    /// unknown archetype, an unknown part, or a slot the part does not belong to. Logs warnings
    /// only - bad data is skipped downstream, never fatal. Runs after all installers so every
    /// catalog exists.
    /// </summary>
    public class MutationContentValidator : IInitializable
    {
        private readonly IArtifactCatalog _artifacts;
        private readonly IArchetypeCatalog _archetypes;
        private readonly IReadOnlyList<ArchetypePartSetDefinition> _partSets;
        private readonly IPartCatalog _parts;
        private readonly IGameLogger _logger;

        public MutationContentValidator(
            IArtifactCatalog artifacts,
            IArchetypeCatalog archetypes,
            IReadOnlyList<ArchetypePartSetDefinition> partSets,
            IPartCatalog parts,
            IGameLogger logger)
        {
            _artifacts = artifacts;
            _archetypes = archetypes;
            _partSets = partSets;
            _parts = parts;
            _logger = logger;
        }

        public void Initialize()
        {
            ValidateArtifactWeights();
            ValidatePartSets();
        }

        private void ValidateArtifactWeights()
        {
            foreach (var artifact in _artifacts.All)
            {
                if (artifact == null || artifact.ArchetypeWeights == null)
                {
                    continue;
                }

                foreach (var weight in artifact.ArchetypeWeights)
                {
                    if (weight == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(weight.ArchetypeId))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Artifact '{artifact.Id}' has an archetype " +
                            "weight with an empty archetype id; it will be ignored.");
                        continue;
                    }

                    if (!_archetypes.Contains(weight.ArchetypeId))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Artifact '{artifact.Id}' references unknown " +
                            $"archetype id '{weight.ArchetypeId}'. Add an ArchetypeDefinition with that " +
                            "id or fix the artifact.");
                    }
                }
            }
        }

        private void ValidatePartSets()
        {
            if (_partSets == null)
            {
                return;
            }

            foreach (var set in _partSets)
            {
                if (set == null)
                {
                    continue;
                }

                if (!_archetypes.Contains(set.ArchetypeId))
                {
                    _logger.Warning(
                        $"[MutationContentValidator] Part set '{set.name}' references unknown archetype " +
                        $"id '{set.ArchetypeId}'. Its options will never be offered.");
                }

                foreach (var option in set.Options)
                {
                    if (option == null || string.IsNullOrEmpty(option.PartId))
                    {
                        continue;
                    }

                    if (!_parts.TryGet(option.PartId, out var part))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Part set '{set.name}' references unknown part " +
                            $"id '{option.PartId}'. The mutation choice will fail to swap it.");
                        continue;
                    }

                    var partSlotId = part.Slot != null ? part.Slot.Id : null;
                    if (!string.Equals(partSlotId, option.SlotId, System.StringComparison.Ordinal))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Part set '{set.name}' option for part " +
                            $"'{option.PartId}' declares slot '{option.SlotId}' but the part belongs to " +
                            $"slot '{partSlotId}'.");
                    }
                }
            }
        }
    }
}
