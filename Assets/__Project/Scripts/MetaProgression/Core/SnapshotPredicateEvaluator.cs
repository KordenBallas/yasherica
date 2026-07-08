using System.Collections.Generic;
using Narrative.Facts.Core;

namespace MetaProgression.Core
{
    /// <summary>
    /// Evaluates authored deed predicates against a serialized meta-fact image (meta-progression
    /// FR4/FR13) — no live fact store, no casting context: the vocabulary is a pure function of the
    /// meta store on disk. Subject binding is deliberately minimal: empty = global fact,
    /// <c>$self</c> = the owning token id, a bare literal = itself (e.g. an explicit part or story
    /// id); any other <c>$</c>-token is unresolvable here and fails closed. Deeds are about facts a
    /// player has EARNED, so a missing fact always fails closed (locked) — unlike the run-time
    /// <see cref="PreconditionEvaluator"/>, no registry default is substituted. Use
    /// <see cref="ComparisonOp.NotExists"/> for an explicit absence test.
    /// </summary>
    public static class SnapshotPredicateEvaluator
    {
        public const string SelfToken = "$self";
        private const char ContextTokenPrefix = '$';

        /// <summary>AND over the list; an empty or null list passes (the precondition convention).</summary>
        public static bool EvaluateAll(
            IReadOnlyList<FactPredicate> predicates,
            IReadOnlyDictionary<FactKey, FactValue> facts,
            string selfSubject)
        {
            if (predicates == null)
            {
                return true;
            }

            for (int i = 0; i < predicates.Count; i++)
            {
                if (!Evaluate(predicates[i], facts, selfSubject))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Evaluate(
            FactPredicate predicate,
            IReadOnlyDictionary<FactKey, FactValue> facts,
            string selfSubject)
        {
            if (predicate == null || facts == null)
            {
                return false;
            }

            if (!TryResolveSubject(predicate.SubjectToken, selfSubject, out var subject))
            {
                return false;
            }

            var key = new FactKey(predicate.Namespace, subject, predicate.Key);
            bool present = facts.TryGetValue(key, out var actual);

            switch (predicate.Op)
            {
                case ComparisonOp.Exists:
                    return present;
                case ComparisonOp.NotExists:
                    return !present;
            }

            if (!present)
            {
                return false;
            }

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
                    return CompareOrdered(predicate.Op, actual, predicate.Value);
                default:
                    return false;
            }
        }

        private static bool TryResolveSubject(string token, string selfSubject, out string subject)
        {
            if (string.IsNullOrEmpty(token))
            {
                subject = string.Empty;
                return true;
            }

            if (token == SelfToken)
            {
                subject = selfSubject ?? string.Empty;
                return true;
            }

            if (token[0] == ContextTokenPrefix)
            {
                // $target/$faction/$location need a casting context the meta snapshot cannot supply.
                subject = string.Empty;
                return false;
            }

            subject = token;
            return true;
        }

        private static bool CompareOrdered(ComparisonOp op, FactValue actual, FactValue expected)
        {
            if (!actual.IsNumeric || !expected.IsNumeric)
            {
                return false;
            }

            int cmp = actual.CompareNumericOrEquatable(expected);
            switch (op)
            {
                case ComparisonOp.Gt: return cmp > 0;
                case ComparisonOp.Gte: return cmp >= 0;
                case ComparisonOp.Lt: return cmp < 0;
                case ComparisonOp.Lte: return cmp <= 0;
                default: return false;
            }
        }
    }
}
