using CharacterSystem.Data;
using Core.Logging;
using Inventory.Data;
using Mutation.Data;
using Zenject;

namespace Mutation.Application
{
    /// <summary>
    /// Startup check that surfaces authoring mistakes: any artifact whose archetype weights
    /// reference an unknown archetype id, and any body part whose mutation affinity references an
    /// empty or unknown archetype id. Logs warnings only - bad data is skipped downstream, never
    /// fatal. Runs after all installers so every catalog exists.
    /// </summary>
    public class MutationContentValidator : IInitializable
    {
        private readonly IArtifactCatalog _artifacts;
        private readonly IArchetypeCatalog _archetypes;
        private readonly IPartCatalog _parts;
        private readonly IGameLogger _logger;

        public MutationContentValidator(
            IArtifactCatalog artifacts,
            IArchetypeCatalog archetypes,
            IPartCatalog parts,
            IGameLogger logger)
        {
            _artifacts = artifacts;
            _archetypes = archetypes;
            _parts = parts;
            _logger = logger;
        }

        public void Initialize()
        {
            ValidateArtifactWeights();
            ValidatePartAffinities();
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

        private void ValidatePartAffinities()
        {
            foreach (var part in _parts.All)
            {
                if (part == null)
                {
                    continue;
                }

                foreach (var affinity in part.ArchetypeAffinities)
                {
                    if (affinity == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(affinity.ArchetypeId))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Part '{part.Id}' has a mutation affinity with " +
                            "an empty archetype id; it will be ignored when scoring.");
                        continue;
                    }

                    if (!_archetypes.Contains(affinity.ArchetypeId))
                    {
                        _logger.Warning(
                            $"[MutationContentValidator] Part '{part.Id}' has a mutation affinity for " +
                            $"unknown archetype id '{affinity.ArchetypeId}'. Add an ArchetypeDefinition " +
                            "with that id or fix the part.");
                    }
                }
            }
        }
    }
}
