using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class SubjectResolverTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        private FakeLogger _logger;
        private SubjectResolver _resolver;
        private SubjectContext _context;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            _resolver = new SubjectResolver(_logger);
            _context = new SubjectContext()
                .Bind("$self", "npc_07")
                .Bind("$faction", "blades")
                .Bind("$location", "razor_pass");
        }

        [Test]
        public void EmptyToken_ResolvesToGlobalSubject()
        {
            Assert.IsTrue(_resolver.TryResolve("", _context, out var subject));
            Assert.AreEqual(string.Empty, subject);
            Assert.IsTrue(_resolver.TryResolve(null, _context, out subject));
            Assert.AreEqual(string.Empty, subject);
        }

        [Test]
        public void BuiltInTokens_ResolveFromContext()
        {
            Assert.IsTrue(_resolver.TryResolve("$self", _context, out var self));
            Assert.AreEqual("npc_07", self);

            Assert.IsTrue(_resolver.TryResolve("$faction", _context, out var faction));
            Assert.AreEqual("blades", faction);
        }

        [Test]
        public void ArbitraryContextKey_Resolves()
        {
            // A1: an open $<contextKey> like $location resolves against the bag.
            Assert.IsTrue(_resolver.TryResolve("$location", _context, out var loc));
            Assert.AreEqual("razor_pass", loc);
        }

        [Test]
        public void UnknownToken_FailsClosedAndWarns()
        {
            Assert.IsFalse(_resolver.TryResolve("$missing", _context, out var subject));
            Assert.AreEqual(string.Empty, subject);
            Assert.AreEqual(1, _logger.Warnings.Count);
        }

        [Test]
        public void LiteralToken_PassesThroughAsConcreteSubject()
        {
            Assert.IsTrue(_resolver.TryResolve("blades", _context, out var subject));
            Assert.AreEqual("blades", subject);
        }

        [Test]
        public void ScopedWorldFact_WriteAndReadRoundTrip_ViaResolvedLocation()
        {
            // A1 acceptance: world.<locationId>.burned authored with a $location token.
            var store = new FactStore();
            Assert.IsTrue(_resolver.TryResolve("$location", _context, out var loc));

            var burned = new FactKey(FactNamespace.World, loc, "burned");
            store.Set(burned, FactValue.FromBool(true));

            Assert.IsTrue(store.GetOrDefault(new FactKey(FactNamespace.World, "razor_pass", "burned"), FactValue.FromBool(false)).AsBool());
            Assert.IsFalse(store.Has(new FactKey(FactNamespace.World, "other_place", "burned")));
        }
    }
}
