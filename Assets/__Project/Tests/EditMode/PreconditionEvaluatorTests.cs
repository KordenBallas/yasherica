using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PreconditionEvaluatorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(string message) { }
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) { }
        }

        private FakeLogger _logger;
        private FactStore _store;
        private PreconditionEvaluator _eval;
        private SubjectContext _context;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "count", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
                new FactKeyInfo(FactNamespace.World, "label", FactScope.Global, FactValueType.String, FactValue.FromString("")),
                new FactKeyInfo(FactNamespace.Actor, "hostile", FactScope.PerActor, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.Faction, "reputation", FactScope.PerFaction, FactValueType.Int, FactValue.FromInt(0))
            });
            _store = new FactStore(registry, _logger);
            _eval = new PreconditionEvaluator(new SubjectResolver(_logger), registry, _logger);
            _context = new SubjectContext().Bind("$self", "npc_07").Bind("$faction", "blades");
        }

        private static FactPredicate Pred(FactNamespace ns, string token, string key, ComparisonOp op, FactValue val)
            => new FactPredicate(ns, token, key, op, val);

        [Test]
        public void Bool_PresenceVsDefaultMatrix()
        {
            var key = FactKey.Global(FactNamespace.World, "pass_cleared");

            // unset: default false participates in Eq; Exists is false.
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(false)), _store, _context));
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(true)), _store, _context));
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Exists, FactValue.FromBool(false)), _store, _context));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.NotExists, FactValue.FromBool(false)), _store, _context));

            // set true.
            _store.Set(key, FactValue.FromBool(true));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(true)), _store, _context));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Exists, FactValue.FromBool(false)), _store, _context));

            // removed: back to default false, Exists false.
            _store.Remove(key);
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Eq, FactValue.FromBool(false)), _store, _context));
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "pass_cleared", ComparisonOp.Exists, FactValue.FromBool(false)), _store, _context));
        }

        [Test]
        public void Int_OrderedOps()
        {
            _store.Set(FactKey.Global(FactNamespace.World, "count"), FactValue.FromInt(5));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "count", ComparisonOp.Gte, FactValue.FromInt(5)), _store, _context));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "count", ComparisonOp.Gt, FactValue.FromInt(4)), _store, _context));
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "count", ComparisonOp.Lt, FactValue.FromInt(5)), _store, _context));
            Assert.IsTrue(_eval.Evaluate(Pred(FactNamespace.World, "", "count", ComparisonOp.Lte, FactValue.FromInt(5)), _store, _context));
        }

        [Test]
        public void OrderedOpOnNonNumeric_FailsClosedAndWarns()
        {
            _store.Set(FactKey.Global(FactNamespace.World, "label"), FactValue.FromString("abc"));
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "label", ComparisonOp.Gt, FactValue.FromString("a")), _store, _context));
            Assert.GreaterOrEqual(_logger.Warnings.Count, 1);
        }

        [Test]
        public void EvaluateAll_IsAnd_AcrossActorAndFaction()
        {
            // R10: an actor predicate and a faction predicate in one uniform pass.
            _store.SetInt(FactionFacts.Reputation, 7, "blades");
            // actor hostile unset -> default false.

            var predicates = new List<FactPredicate>
            {
                Pred(FactNamespace.Actor, "$self", "hostile", ComparisonOp.Eq, FactValue.FromBool(false)),
                Pred(FactNamespace.Faction, "$faction", "reputation", ComparisonOp.Gte, FactValue.FromInt(5))
            };

            Assert.IsTrue(_eval.EvaluateAll(predicates, _store, _context));

            // Make the actor hostile -> the AND fails.
            _store.SetBool(ActorFacts.Hostile, true, "npc_07");
            Assert.IsFalse(_eval.EvaluateAll(predicates, _store, _context));
        }

        [Test]
        public void UnknownKey_FailsClosedAndWarns()
        {
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.World, "", "not_in_vocab", ComparisonOp.Eq, FactValue.FromBool(false)), _store, _context));
            Assert.GreaterOrEqual(_logger.Warnings.Count, 1);
        }

        [Test]
        public void UnresolvedSubjectToken_FailsClosed()
        {
            Assert.IsFalse(_eval.Evaluate(Pred(FactNamespace.Actor, "$missing", "hostile", ComparisonOp.Eq, FactValue.FromBool(false)), _store, _context));
        }

        [Test]
        public void NullPredicateList_PassesVacuously()
        {
            Assert.IsTrue(_eval.EvaluateAll(null, _store, _context));
        }
    }
}
