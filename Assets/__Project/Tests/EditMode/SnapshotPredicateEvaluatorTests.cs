using MetaProgression.Core;
using Narrative.Facts.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Tests.EditMode
{
    [TestFixture]
    public class SnapshotPredicateEvaluatorTests
    {
        private static Dictionary<FactKey, FactValue> Facts(params (FactKey key, FactValue value)[] entries)
        {
            var facts = new Dictionary<FactKey, FactValue>();
            foreach (var (key, value) in entries)
            {
                facts[key] = value;
            }

            return facts;
        }

        private static FactPredicate Pred(string subjectToken, string key, ComparisonOp op, FactValue value) =>
            new FactPredicate(FactNamespace.World, subjectToken, key, op, value);

        [Test]
        public void SelfToken_BindsToOwningTokenId()
        {
            var facts = Facts((new FactKey(FactNamespace.World, "part_serpent", "arena_tasted"), FactValue.FromBool(true)));
            var deed = new[] { Pred("$self", "arena_tasted", ComparisonOp.Eq, FactValue.FromBool(true)) };

            Assert.IsTrue(SnapshotPredicateEvaluator.EvaluateAll(deed, facts, "part_serpent"));
            Assert.IsFalse(SnapshotPredicateEvaluator.EvaluateAll(deed, facts, "part_other"));
        }

        [Test]
        public void LiteralSubject_ResolvesToItself()
        {
            var facts = Facts((new FactKey(FactNamespace.World, "story_mirror", "spine_seen"), FactValue.FromBool(true)));
            var deed = new[] { Pred("story_mirror", "spine_seen", ComparisonOp.Eq, FactValue.FromBool(true)) };

            Assert.IsTrue(SnapshotPredicateEvaluator.EvaluateAll(deed, facts, "unrelated_token"));
        }

        [Test]
        public void GlobalSubject_ReadsGlobalFact()
        {
            var facts = Facts((FactKey.Global(FactNamespace.World, "run_count"), FactValue.FromInt(5)));

            Assert.IsTrue(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "run_count", ComparisonOp.Gte, FactValue.FromInt(3)), facts, "any"));
            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "run_count", ComparisonOp.Gte, FactValue.FromInt(6)), facts, "any"));
        }

        [Test]
        public void UnresolvableContextToken_FailsClosed()
        {
            var facts = Facts((new FactKey(FactNamespace.World, "x", "flag"), FactValue.FromBool(true)));

            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred("$target", "flag", ComparisonOp.Eq, FactValue.FromBool(true)), facts, "x"));
        }

        [Test]
        public void MissingFact_FailsClosedOnValueOps()
        {
            var facts = Facts();

            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "run_count", ComparisonOp.Gte, FactValue.FromInt(0)), facts, "any"));
            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "flag", ComparisonOp.NotEq, FactValue.FromBool(true)), facts, "any"));
        }

        [Test]
        public void ExistsAndNotExists_TestPresenceOnly()
        {
            var facts = Facts((FactKey.Global(FactNamespace.World, "flag"), FactValue.FromBool(false)));

            Assert.IsTrue(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "flag", ComparisonOp.Exists, FactValue.FromBool(true)), facts, "any"));
            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "flag", ComparisonOp.NotExists, FactValue.FromBool(true)), facts, "any"));
            Assert.IsTrue(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "absent", ComparisonOp.NotExists, FactValue.FromBool(true)), facts, "any"));
        }

        [Test]
        public void OrderedOpOnNonNumeric_FailsClosed()
        {
            var facts = Facts((FactKey.Global(FactNamespace.World, "name"), FactValue.FromString("abc")));

            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "name", ComparisonOp.Gt, FactValue.FromString("a")), facts, "any"));
        }

        [Test]
        public void EmptyOrNullDeed_Passes()
        {
            var facts = Facts();

            Assert.IsTrue(SnapshotPredicateEvaluator.EvaluateAll(null, facts, "any"));
            Assert.IsTrue(SnapshotPredicateEvaluator.EvaluateAll(new FactPredicate[0], facts, "any"));
        }

        [Test]
        public void NullPredicateOrFacts_FailClosed()
        {
            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(null, Facts(), "any"));
            Assert.IsFalse(SnapshotPredicateEvaluator.Evaluate(
                Pred(string.Empty, "flag", ComparisonOp.Exists, FactValue.FromBool(true)), null, "any"));
        }
    }
}
