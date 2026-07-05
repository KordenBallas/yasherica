using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Threads.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free description of one authored thread (mapped from the
    /// <c>ThreadDefinition</c> ScriptableObject). Premise predicates must all hold for the thread to
    /// stay viable — a live thread whose premise evaluates false is retired as failed/conflict
    /// (FR5). Resolution conditions, when present, mark the thread's payoff: once they hold, the
    /// thread retires as resolved. Both are world-scoped only (no <c>$</c> context tokens — there is
    /// no casting to resolve them against at the planning tick).
    /// </summary>
    public sealed class ThreadDefinitionData
    {
        public string ThreadId { get; }
        public ThreadKind Kind { get; }
        public IReadOnlyList<FactPredicate> Premise { get; }
        public IReadOnlyList<FactPredicate> ResolutionConditions { get; }

        /// <summary>Windows the thread may go without advancing before it expires. Ignored for
        /// <see cref="ThreadKind.Arc"/> threads (expiry-exempt, D13).</summary>
        public int LifespanWindows { get; }

        public ThreadDefinitionData(string threadId, ThreadKind kind,
            IReadOnlyList<FactPredicate> premise, IReadOnlyList<FactPredicate> resolutionConditions,
            int lifespanWindows)
        {
            ThreadId = threadId ?? string.Empty;
            Kind = kind;
            Premise = premise ?? System.Array.Empty<FactPredicate>();
            ResolutionConditions = resolutionConditions ?? System.Array.Empty<FactPredicate>();
            LifespanWindows = lifespanWindows < 1 ? 1 : lifespanWindows;
        }
    }
}
