using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class TypedFactsTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(string message) { }
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) { }
        }

        [Test]
        public void TypedExtensions_RoundTripGlobalAndScopedValues()
        {
            var store = new FactStore();

            store.SetBool(WorldFacts.PassCleared, true);
            Assert.IsTrue(store.GetBool(WorldFacts.PassCleared));

            store.SetBool(ActorFacts.Hostile, true, "npc_07");
            Assert.IsTrue(store.GetBool(ActorFacts.Hostile, "npc_07"));
            Assert.IsFalse(store.GetBool(ActorFacts.Hostile, "npc_99")); // different subject, unset

            store.SetInt(FactionFacts.Reputation, 5, "blades");
            Assert.AreEqual(5, store.GetInt(FactionFacts.Reputation, "blades"));
        }

        [Test]
        public void GetBool_ReturnsTypeDefault_WhenUnset()
        {
            var store = new FactStore();
            Assert.IsFalse(store.GetBool(WorldFacts.PassBlocked));
            Assert.AreEqual(0, store.GetInt(FactionFacts.Reputation, "any"));
        }

        [Test]
        public void DriftCheck_PassesWhenAllRefsDeclared()
        {
            var logger = new FakeLogger();
            var registry = BuildRegistryFromRefs(TypedFacts.All());

            Assert.IsTrue(FactKeyRefRegistryCheck.Validate(registry, TypedFacts.All(), logger));
            Assert.AreEqual(0, logger.Warnings.Count);
        }

        [Test]
        public void DriftCheck_FlagsMissingRef()
        {
            var logger = new FakeLogger();
            // Registry missing ActorFacts.Hostile.
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_blocked", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.Faction, "reputation", FactScope.PerFaction, FactValueType.Int, FactValue.FromInt(0))
            });

            Assert.IsFalse(FactKeyRefRegistryCheck.Validate(registry, TypedFacts.All(), logger));
            Assert.AreEqual(1, logger.Warnings.Count);
        }

        [Test]
        public void DriftCheck_FlagsTypeMismatch()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                // pass_cleared declared as Int instead of Bool.
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Int, FactValue.FromInt(0))
            });

            Assert.IsFalse(FactKeyRefRegistryCheck.Validate(registry, new[] { WorldFacts.PassCleared }, logger));
            Assert.AreEqual(1, logger.Warnings.Count);
        }

        private static FactKeyRegistry BuildRegistryFromRefs(IEnumerable<FactKeyRef> refs)
        {
            var infos = new List<FactKeyInfo>();
            foreach (var r in refs)
            {
                infos.Add(new FactKeyInfo(r.Namespace, r.Key, r.Scope, r.ValueType, FactValue.DefaultFor(r.ValueType)));
            }

            return new FactKeyRegistry(infos);
        }
    }
}
