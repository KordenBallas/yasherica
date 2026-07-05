using System.Collections.Generic;
using System.Reflection;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using LevelGeneration;
using Narrative.Facts.Core;
using NUnit.Framework;
using UnityEngine;
using World.Races.Core;
using World.Races.Integration;

namespace Tests.EditMode
{
    [TestFixture]
    public class RacePassportProjectorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public int Warnings;
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings++;
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakePartCatalog : IPartCatalog
        {
            private readonly Dictionary<string, PartDefinition> _parts =
                new Dictionary<string, PartDefinition>();

            public void Add(PartDefinition part) => _parts[part.Id] = part;

            public IReadOnlyList<PartDefinition> All => new List<PartDefinition>(_parts.Values);

            public bool TryGet(string partId, out PartDefinition definition) =>
                _parts.TryGetValue(partId ?? string.Empty, out definition);
        }

        private readonly List<Object> _created = new List<Object>();
        private FakeLogger _logger;
        private FactStore _store;
        private FactKeyRegistry _registry;
        private FakePartCatalog _catalog;
        private IRaceRoster _roster;
        private RacePassportProjector _projector;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            _registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.Faction, "reads_as_tier", FactScope.PerFaction,
                    FactValueType.Int, FactValue.FromInt(0))
            });
            _store = new FactStore(_registry, _logger);
            _catalog = new FakePartCatalog();
            _catalog.Add(Part("part.head.a", ""));
            _catalog.Add(Part("part.head.b", "ibex"));
            _catalog.Add(Part("part.tail.b", "fox"));
            _catalog.Add(Part("part.legl.b", "fox"));
            _catalog.Add(Part("part.torso.b", "lizard"));
            _roster = new RaceRoster(new[]
            {
                new RaceData("ibex", "Ibex-folk", LevelTheme.Mountain),
                new RaceData("lizard", "Lizard-folk", LevelTheme.Desert),
                new RaceData("fox", "Fox-folk", LevelTheme.Forest)
            });
            _projector = new RacePassportProjector(_roster, _catalog, _store, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private PartDefinition Part(string id, string raceId)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            _created.Add(part);
            typeof(PartDefinition).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(part, id);
            typeof(PartDefinition).GetField("_raceId", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(part, raceId);
            return part;
        }

        private long Tier(string raceId) => _store.GetInt(FactionFacts.ReadsAsTier, raceId);

        [Test]
        public void KindlessBody_WritesTierZero_ForEveryRosterRace()
        {
            _projector.Recompute(new[] { "part.head.a" });

            Assert.AreEqual(0, Tier("ibex"));
            Assert.AreEqual(0, Tier("lizard"));
            Assert.AreEqual(0, Tier("fox"));
            Assert.IsTrue(_store.Has(FactionFacts.ReadsAsTier.ToKey("fox")), "tier is written, not just defaulted");
        }

        [Test]
        public void MixedBody_TiersFollowPartCounts()
        {
            _projector.Recompute(new[] { "part.head.a", "part.tail.b", "part.legl.b", "part.torso.b" });

            Assert.AreEqual(2, Tier("fox"));
            Assert.AreEqual(1, Tier("lizard"));
            Assert.AreEqual(0, Tier("ibex"));
        }

        [Test]
        public void Recompute_AfterSwapAway_TierDropsBackToZero()
        {
            _projector.Recompute(new[] { "part.tail.b" });
            Assert.AreEqual(1, Tier("fox"));

            _projector.Recompute(new[] { "part.head.b" });

            Assert.AreEqual(0, Tier("fox"));
            Assert.AreEqual(1, Tier("ibex"));
        }

        [Test]
        public void UnknownPartId_TreatedKindless_Warns()
        {
            _projector.Recompute(new[] { "part.missing", "part.tail.b" });

            Assert.AreEqual(1, Tier("fox"));
            Assert.AreEqual(1, _logger.Warnings);
        }

        [Test]
        public void GatingIntegration_FoxTierGte1_FlipsAfterOneFoxPart()
        {
            var evaluator = new PreconditionEvaluator(new SubjectResolver(_logger), _registry, _logger);
            // The authored demo gate (DemoStory_FrogElderOpen): a literal race-id subject token.
            var openGate = new FactPredicate(
                FactNamespace.Faction, "fox", "reads_as_tier", ComparisonOp.Gte, FactValue.FromInt(1));
            var closedGate = new FactPredicate(
                FactNamespace.Faction, "fox", "reads_as_tier", ComparisonOp.Lt, FactValue.FromInt(1));

            _projector.Recompute(new[] { "part.head.a" });
            Assert.IsFalse(evaluator.Evaluate(openGate, _store, null));
            Assert.IsTrue(evaluator.Evaluate(closedGate, _store, null));

            _projector.Recompute(new[] { "part.head.a", "part.tail.b" });
            Assert.IsTrue(evaluator.Evaluate(openGate, _store, null));
            Assert.IsFalse(evaluator.Evaluate(closedGate, _store, null));
        }
    }
}
