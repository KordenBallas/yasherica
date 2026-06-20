using System.Collections.Generic;
using Core.Logging;
using Narrative.Casting.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Zenject;

namespace Narrative.Runtime.Core
{
    /// <summary>
    /// Runs once after the container is built: derives and caches each story template's advisory effect
    /// footprint over the fragment library (W3-2), and validates the curated typed fact refs against the
    /// authoritative registry (D3). Doing this in <see cref="IInitializable"/> avoids resolving services
    /// during install.
    /// </summary>
    public sealed class NarrativeSliceBootstrap : IInitializable
    {
        private readonly ICastingFactory _castingFactory;
        private readonly IFragmentLibrary _library;
        private readonly IReadOnlyList<StoryTemplateData> _storylets;
        private readonly IFactKeyRegistry _registry;
        private readonly IGameLogger _logger;

        public NarrativeSliceBootstrap(ICastingFactory castingFactory, IFragmentLibrary library,
            IReadOnlyList<StoryTemplateData> storylets, IFactKeyRegistry registry, IGameLogger logger)
        {
            _castingFactory = castingFactory;
            _library = library;
            _storylets = storylets;
            _registry = registry;
            _logger = logger;
        }

        public void Initialize()
        {
            FactKeyRefRegistryCheck.Validate(_registry, TypedFacts.All(), _logger);

            foreach (var story in _storylets)
            {
                _castingFactory.DeriveFootprint(story, _library);
            }
        }
    }
}
