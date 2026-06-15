using Core.Logging;
using Inventory.Data;
using Mutation.Data;
using Zenject;

namespace Mutation.Application
{
    /// <summary>
    /// Startup check that surfaces authoring mistakes: any artifact whose archetype weights
    /// reference an unknown archetype id, or carry an empty id. Logs warnings only - bad data
    /// is skipped downstream, never fatal. Runs after all installers so both catalogs exist.
    /// </summary>
    public class MutationContentValidator : IInitializable
    {
        private readonly IArtifactCatalog _artifacts;
        private readonly IArchetypeCatalog _archetypes;
        private readonly IGameLogger _logger;

        public MutationContentValidator(
            IArtifactCatalog artifacts,
            IArchetypeCatalog archetypes,
            IGameLogger logger)
        {
            _artifacts = artifacts;
            _archetypes = archetypes;
            _logger = logger;
        }

        public void Initialize()
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
    }
}
