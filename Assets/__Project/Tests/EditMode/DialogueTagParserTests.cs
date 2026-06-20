using System.Collections.Generic;
using Core.Logging;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DialogueTagParserTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(string message) { }
            public void Warning(string message) => Warnings.Add(message);
            public void Error(string message) { }
        }

        private FakeLogger _logger;
        private DialogueTagParser _parser;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.Faction, "reputation", FactScope.PerFaction, FactValueType.Int, FactValue.FromInt(0)),
                new FactKeyInfo(FactNamespace.Actor, "hostile", FactScope.PerActor, FactValueType.Bool, FactValue.FromBool(false))
            });
            _parser = new DialogueTagParser(registry, _logger);
        }

        [Test]
        public void Parses_SimpleBridgeTags()
        {
            Assert.AreEqual(DialogueTagKind.Speaker, _parser.Parse("speaker: Razor").Kind);
            Assert.AreEqual("Razor", _parser.Parse("speaker: Razor").Argument);
            Assert.AreEqual(DialogueTagKind.OfferQuest, _parser.Parse("offer-quest: errand").Kind);
            Assert.AreEqual(DialogueTagKind.StartCombat, _parser.Parse("start-combat: bandit").Kind);
            Assert.AreEqual(DialogueTagKind.Outcome, _parser.Parse("outcome: trade").Kind);
        }

        [Test]
        public void ParsesGlobalBoolFact()
        {
            var tag = _parser.Parse("fact: world.pass_cleared Set true");
            Assert.AreEqual(DialogueTagKind.Fact, tag.Kind);
            Assert.AreEqual(FactNamespace.World, tag.Effect.Namespace);
            Assert.AreEqual("pass_cleared", tag.Effect.Key);
            Assert.AreEqual(string.Empty, tag.Effect.SubjectToken);
            Assert.AreEqual(FactEffectOp.Set, tag.Effect.Op);
            Assert.IsTrue(tag.Effect.Value.AsBool());
        }

        [Test]
        public void ParsesScopedActorFact_WithSubjectToken()
        {
            var tag = _parser.Parse("fact: actor.$self.hostile Set true");
            Assert.AreEqual(DialogueTagKind.Fact, tag.Kind);
            Assert.AreEqual(FactNamespace.Actor, tag.Effect.Namespace);
            Assert.AreEqual("$self", tag.Effect.SubjectToken);
            Assert.AreEqual("hostile", tag.Effect.Key);
        }

        [Test]
        public void ParsesIntAddFact_TypedViaRegistry()
        {
            var tag = _parser.Parse("fact: faction.$faction.reputation Add 5");
            Assert.AreEqual(FactEffectOp.Add, tag.Effect.Op);
            Assert.AreEqual(5, tag.Effect.Value.AsInt());
            Assert.AreEqual(FactValueType.Int, tag.Effect.Value.Type);
        }

        [Test]
        public void UnknownKeyFact_FailsClosedAndWarns()
        {
            var tag = _parser.Parse("fact: world.not_in_vocab Set true");
            Assert.AreEqual(DialogueTagKind.Unknown, tag.Kind);
            Assert.AreEqual(1, _logger.Warnings.Count);
        }

        [Test]
        public void MalformedFact_FailsClosed()
        {
            Assert.AreEqual(DialogueTagKind.Unknown, _parser.Parse("fact: world.pass_cleared").Kind);
            Assert.AreEqual(DialogueTagKind.Unknown, _parser.Parse("fact: badpath Set true").Kind);
        }

        [Test]
        public void UnrecognizedTag_IsUnknown_NotWarned()
        {
            Assert.AreEqual(DialogueTagKind.Unknown, _parser.Parse("random: stuff").Kind);
            Assert.AreEqual(DialogueTagKind.Unknown, _parser.Parse("noColon").Kind);
        }
    }
}
