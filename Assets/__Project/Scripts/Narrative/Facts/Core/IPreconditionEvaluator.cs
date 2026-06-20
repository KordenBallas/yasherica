using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Evaluates fact preconditions uniformly across all namespaces (R9). Generalizes the legacy
    /// single-predicate RunConditionEvaluator: resolves the subject token, reads the store, and
    /// applies the comparison with the B4 presence/default contract. <see cref="EvaluateAll"/> is the
    /// AND of all predicates (OR composition is deferred).
    /// </summary>
    public interface IPreconditionEvaluator
    {
        bool Evaluate(FactPredicate predicate, IFactStore store, ISubjectContext context);
        bool EvaluateAll(IReadOnlyList<FactPredicate> predicates, IFactStore store, ISubjectContext context);
    }
}
