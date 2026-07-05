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
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        // A per-faction Int ref for exercising the typed Int accessors; not part of TypedFacts.All()
        // (the barn slice has no faction/int fact), so it never participates in the registry drift check.
        private static readonly FactKeyRef Reputation =
            new FactKeyRef(FactNamespace.Faction, FactScope.PerFaction, "reputation", FactValueType.Int);

        [Test]
        public void TypedExtensions_RoundTripGlobalAndScopedValues()
        {
            var store = new FactStore();

            store.SetBool(WorldFacts.BarnRaided, true);
            Assert.IsTrue(store.GetBool(WorldFacts.BarnRaided));

            store.SetBool(ActorFacts.LootedBarn, true, "npc_07");
            Assert.IsTrue(store.GetBool(ActorFacts.LootedBarn, "npc_07"));
            Assert.IsFalse(store.GetBool(ActorFacts.LootedBarn, "npc_99")); // different subject, unset

            store.SetInt(Reputation, 5, "blades");
            Assert.AreEqual(5, store.GetInt(Reputation, "blades"));
        }

        [Test]
        public void GetBool_ReturnsTypeDefault_WhenUnset()
        {
            var store = new FactStore();
            Assert.IsFalse(store.GetBool(WorldFacts.GrainRecovered));
            Assert.AreEqual(0, store.GetInt(Reputation, "any"));
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
            // Registry declares only these two; every other curated ref must be flagged, one warning
            // each. Derived from TypedFacts.All() so adding a new curated ref doesn't break this test
            // (it silently expected exactly one missing ref before run_escalation_tier/reads_as_tier).
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "barn_raided", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "grain_recovered", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false))
            });

            var expectedMissing = 0;
            foreach (var _ in TypedFacts.All())
            {
                expectedMissing++;
            }

            expectedMissing -= 2; // the two declared above

            Assert.IsFalse(FactKeyRefRegistryCheck.Validate(registry, TypedFacts.All(), logger));
            Assert.AreEqual(expectedMissing, logger.Warnings.Count);
        }

        [Test]
        public void DriftCheck_FlagsTypeMismatch()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                // barn_raided declared as Int instead of Bool.
                new FactKeyInfo(FactNamespace.World, "barn_raided", FactScope.Global, FactValueType.Int, FactValue.FromInt(0))
            });

            Assert.IsFalse(FactKeyRefRegistryCheck.Validate(registry, new[] { WorldFacts.BarnRaided }, logger));
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
