using System.Collections.Generic;
using Core.Logging;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Default <see cref="IPreconditionEvaluator"/>. For each predicate: resolve the subject token →
    /// build the <see cref="FactKey"/> → apply the operator.
    ///
    /// B4 presence/default contract: <see cref="ComparisonOp.Exists"/>/<see cref="ComparisonOp.NotExists"/>
    /// test <see cref="IFactStore.Has"/> (presence only); the value ops read <see cref="IFactStore.GetOrDefault"/>
    /// with the registry default so an unset fact compares as its default. Ordered ops on non-numeric
    /// values, and unknown keys, fail closed (return false) and warn — never over-grant.
    /// </summary>
    public sealed class PreconditionEvaluator : IPreconditionEvaluator
    {
        private readonly ISubjectResolver _subjects;
        private readonly IFactKeyRegistry _registry;
        private readonly IGameLogger _logger;

        public PreconditionEvaluator(ISubjectResolver subjects, IFactKeyRegistry registry, IGameLogger logger = null)
        {
            _subjects = subjects;
            _registry = registry;
            _logger = logger;
        }

        public bool EvaluateAll(IReadOnlyList<FactPredicate> predicates, IFactStore store, ISubjectContext context)
        {
            if (predicates == null)
            {
                return true;
            }

            for (int i = 0; i < predicates.Count; i++)
            {
                if (!Evaluate(predicates[i], store, context))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Evaluate(FactPredicate predicate, IFactStore store, ISubjectContext context)
        {
            if (predicate == null || store == null)
            {
                return false;
            }

            if (!_subjects.TryResolve(predicate.SubjectToken, context, out var subject))
            {
                return false; // resolver already warned
            }

            // Unknown keys are an authoring error - fail closed so a precondition never silently
            // passes on a non-existent fact (only enforced when a registry is supplied).
            if (_registry != null && !_registry.TryGetInfo(predicate.Namespace, predicate.Key, out _))
            {
                _logger?.Warning($"[PreconditionEvaluator] Unknown fact key '{predicate.Namespace}.{predicate.Key}' - failing closed.");
                return false;
            }

            var key = new FactKey(predicate.Namespace, subject, predicate.Key);

            switch (predicate.Op)
            {
                case ComparisonOp.Exists:
                    return store.Has(key);
                case ComparisonOp.NotExists:
                    return !store.Has(key);
            }

            // Value comparisons participate the registry default when the fact is unset (B4).
            var fallback = DefaultFor(predicate);
            var actual = store.GetOrDefault(key, fallback);

            switch (predicate.Op)
            {
                case ComparisonOp.Eq:
                    return actual.Equals(predicate.Value);
                case ComparisonOp.NotEq:
                    return !actual.Equals(predicate.Value);
                case ComparisonOp.Gt:
                case ComparisonOp.Gte:
                case ComparisonOp.Lt:
                case ComparisonOp.Lte:
                    return CompareOrdered(predicate, actual, key);
                default:
                    _logger?.Warning($"[PreconditionEvaluator] Unsupported op {predicate.Op} on '{key}' - failing closed.");
                    return false;
            }
        }

        private bool CompareOrdered(FactPredicate predicate, FactValue actual, FactKey key)
        {
            if (!actual.IsNumeric || !predicate.Value.IsNumeric)
            {
                _logger?.Warning($"[PreconditionEvaluator] Ordered op {predicate.Op} on non-numeric '{key}' - failing closed.");
                return false;
            }

            int cmp = actual.CompareNumericOrEquatable(predicate.Value);
            switch (predicate.Op)
            {
                case ComparisonOp.Gt: return cmp > 0;
                case ComparisonOp.Gte: return cmp >= 0;
                case ComparisonOp.Lt: return cmp < 0;
                case ComparisonOp.Lte: return cmp <= 0;
                default: return false;
            }
        }

        private FactValue DefaultFor(FactPredicate predicate)
        {
            if (_registry != null && _registry.TryGetInfo(predicate.Namespace, predicate.Key, out var info))
            {
                return info.DefaultValue;
            }

            // No registry / unknown key: fall back to the literal's own type default so comparison is
            // well-typed. (The store also fails closed on unknown writes, so this stays consistent.)
            return FactValue.DefaultFor(predicate.Value.Type);
        }
    }
}
