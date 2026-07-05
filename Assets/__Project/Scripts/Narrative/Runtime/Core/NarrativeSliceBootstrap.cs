using System.Collections.Generic;
using Core.Logging;
using Narrative.Casting.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using Zenject;

namespace Narrative.Runtime.Core
{
    /// <summary>
    /// Runs once after the container is built: derives and caches each story template's advisory effect
    /// footprint over the fragment library (W3-2), validates the curated typed fact refs against the
    /// authoritative registry (D3), and surfaces thread-authoring gaps (a story's thread label with no
    /// ThreadDefinition asset falls back to the implicit ephemeral default — legal, but worth a note,
    /// since such a thread can silently expire). Doing this in <see cref="IInitializable"/> avoids
    /// resolving services during install.
    /// </summary>
    public sealed class NarrativeSliceBootstrap : IInitializable
    {
        private readonly ICastingFactory _castingFactory;
        private readonly IFragmentLibrary _library;
        private readonly IReadOnlyList<StoryTemplateData> _storylets;
        private readonly IFactKeyRegistry _registry;
        private readonly IThreadCatalog _threadCatalog;
        private readonly IGameLogger _logger;

        public NarrativeSliceBootstrap(ICastingFactory castingFactory, IFragmentLibrary library,
            IReadOnlyList<StoryTemplateData> storylets, IFactKeyRegistry registry,
            IThreadCatalog threadCatalog, IGameLogger logger)
        {
            _castingFactory = castingFactory;
            _library = library;
            _storylets = storylets;
            _registry = registry;
            _threadCatalog = threadCatalog;
            _logger = logger;
        }

        public void Initialize()
        {
            FactKeyRefRegistryCheck.Validate(_registry, TypedFacts.All(), _logger);

            var warnedThreads = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var story in _storylets)
            {
                _castingFactory.DeriveFootprint(story, _library);

                if (!string.IsNullOrEmpty(story.ThreadId) &&
                    !_threadCatalog.TryGet(story.ThreadId, out _) &&
                    warnedThreads.Add(story.ThreadId))
                {
                    _logger?.Info(LogCategory.Narrative,
                        $"[NarrativeSliceBootstrap] Thread label '{story.ThreadId}' has no ThreadDefinition asset - " +
                        "it runs as an implicit ephemeral thread with the default lifespan.");
                }
            }
        }
    }
}
