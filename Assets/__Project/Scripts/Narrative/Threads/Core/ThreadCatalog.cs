using System.Collections.Generic;

namespace Narrative.Threads.Core
{
    /// <summary>Default <see cref="IThreadCatalog"/>: an immutable id → definition lookup built once
    /// at install time from the mapped <c>ThreadDefinition</c> assets.</summary>
    public sealed class ThreadCatalog : IThreadCatalog
    {
        private readonly Dictionary<string, ThreadDefinitionData> _definitions =
            new Dictionary<string, ThreadDefinitionData>(System.StringComparer.Ordinal);
        private readonly int _defaultLifespanWindows;

        public ThreadCatalog(IEnumerable<ThreadDefinitionData> definitions, int defaultLifespanWindows)
        {
            _defaultLifespanWindows = defaultLifespanWindows < 1 ? 1 : defaultLifespanWindows;
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.ThreadId))
                {
                    continue;
                }

                _definitions[definition.ThreadId] = definition;
            }
        }

        public bool TryGet(string threadId, out ThreadDefinitionData definition)
        {
            if (string.IsNullOrEmpty(threadId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(threadId, out definition);
        }

        public ThreadDefinitionData GetOrImplicitDefault(string threadId)
        {
            return TryGet(threadId, out var definition)
                ? definition
                : new ThreadDefinitionData(threadId, ThreadKind.Ephemeral, null, null, _defaultLifespanWindows);
        }
    }
}
