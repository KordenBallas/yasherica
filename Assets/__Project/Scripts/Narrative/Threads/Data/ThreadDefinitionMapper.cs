using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using Narrative.Threads.Core;

namespace Narrative.Threads.Data
{
    /// <summary>
    /// The only bridge from the <see cref="ThreadDefinition"/> SO to the UnityEngine-free
    /// <see cref="ThreadDefinitionData"/> / <see cref="ThreadCatalog"/> (CLAUDE.md §7). Premise and
    /// resolution predicates are evaluated at the planning tick with no casting context, so a
    /// <c>$</c>-token subject can never resolve there — the mapper warns and keeps the predicate
    /// (which then fails closed) rather than silently dropping the authored intent.
    /// </summary>
    public static class ThreadDefinitionMapper
    {
        public static ThreadDefinitionData ToData(ThreadDefinition definition, IGameLogger logger = null)
        {
            if (definition == null)
            {
                return null;
            }

            return new ThreadDefinitionData(
                definition.ThreadId,
                definition.Kind,
                MapPredicates(definition.Premise, definition.ThreadId, "premise", logger),
                MapPredicates(definition.ResolutionConditions, definition.ThreadId, "resolution", logger),
                definition.LifespanWindows);
        }

        public static ThreadCatalog ToCatalog(IEnumerable<ThreadDefinition> definitions,
            int defaultLifespanWindows, IGameLogger logger = null)
        {
            var mapped = new List<ThreadDefinitionData>();
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    var data = ToData(definition, logger);
                    if (data != null)
                    {
                        mapped.Add(data);
                    }
                }
            }

            return new ThreadCatalog(mapped, defaultLifespanWindows);
        }

        private static IReadOnlyList<FactPredicate> MapPredicates(
            IReadOnlyList<Facts.Data.FactPredicateSerial> serials, string threadId, string block,
            IGameLogger logger)
        {
            if (serials == null || serials.Count == 0)
            {
                return System.Array.Empty<FactPredicate>();
            }

            var predicates = new List<FactPredicate>(serials.Count);
            for (int i = 0; i < serials.Count; i++)
            {
                if (serials[i] == null)
                {
                    continue;
                }

                var predicate = serials[i].ToCore();
                if (!string.IsNullOrEmpty(predicate.SubjectToken) && predicate.SubjectToken[0] == '$')
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[ThreadDefinitionMapper] Thread '{threadId}' {block} predicate " +
                        $"'{predicate.Namespace}.{predicate.SubjectToken}.{predicate.Key}' uses a context token - " +
                        "thread predicates are world-scoped and this will fail closed at the planning tick.");
                }

                predicates.Add(predicate);
            }

            return predicates;
        }
    }
}
