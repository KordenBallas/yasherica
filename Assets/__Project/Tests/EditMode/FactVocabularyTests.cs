using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FactVocabularyTests
    {
        [Test]
        public void Build_ProducesValueOfRequestedType()
        {
            Assert.AreEqual(FactValueType.Bool, FactValueConversion.Build(FactValueType.Bool, true, 0, 0, "").Type);
            Assert.AreEqual(5, FactValueConversion.Build(FactValueType.Int, false, 5, 0, "").AsInt());
            Assert.AreEqual(2.5d, FactValueConversion.Build(FactValueType.Float, false, 0, 2.5f, "").AsFloat(), 0.0001);
            Assert.AreEqual("hi", FactValueConversion.Build(FactValueType.String, false, 0, 0, "hi").AsString());
        }

        [Test]
        public void TryParse_ParsesValidLiterals()
        {
            Assert.IsTrue(FactValueConversion.TryParse(FactValueType.Bool, "true", out var b) && b.AsBool());
            Assert.IsTrue(FactValueConversion.TryParse(FactValueType.Int, "42", out var i) && i.AsInt() == 42);
            Assert.IsTrue(FactValueConversion.TryParse(FactValueType.Float, "1.5", out var f) && f.AsFloat() == 1.5d);
            Assert.IsTrue(FactValueConversion.TryParse(FactValueType.String, "anything", out var s) && s.AsString() == "anything");
        }

        [Test]
        public void TryParse_FailsClosedOnMalformedNumericOrBool()
        {
            Assert.IsFalse(FactValueConversion.TryParse(FactValueType.Int, "notanint", out _));
            Assert.IsFalse(FactValueConversion.TryParse(FactValueType.Bool, "yes", out _));
            Assert.IsFalse(FactValueConversion.TryParse(FactValueType.Float, "x", out _));
        }

        [Test]
        public void Registry_FindsDeclaredKeys_AndMissesUnknown()
        {
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.Faction, "reputation", FactScope.PerFaction, FactValueType.Int, FactValue.FromInt(0))
            });

            Assert.IsTrue(registry.TryGetInfo(FactNamespace.World, "pass_cleared", out var info));
            Assert.AreEqual(FactScope.Global, info.Scope);
            Assert.AreEqual(FactValueType.Bool, info.ValueType);

            Assert.IsFalse(registry.TryGetInfo(FactNamespace.World, "unknown", out _));
            // Same key name in a different namespace is a different key.
            Assert.IsFalse(registry.TryGetInfo(FactNamespace.Actor, "reputation", out _));
            Assert.AreEqual(2, registry.Keys.Count);
        }

        [Test]
        public void KeyInfo_HorizonDefaultsToRun_AndCarriesMeta()
        {
            // D20: an undeclared horizon is run-scoped, so every pre-existing key stays run-scoped.
            var defaulted = new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global,
                FactValueType.Bool, FactValue.FromBool(false));
            Assert.AreEqual(FactHorizon.Run, defaulted.Horizon);

            var meta = new FactKeyInfo(FactNamespace.World, "spine_cursor", FactScope.Global,
                FactValueType.Int, FactValue.FromInt(0), FactHorizon.Meta);
            Assert.AreEqual(FactHorizon.Meta, meta.Horizon);
        }

        [Test]
        public void Registry_PreservesHorizonPerKey()
        {
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "run_fact", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "meta_fact", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false), FactHorizon.Meta)
            });

            Assert.IsTrue(registry.TryGetInfo(FactNamespace.World, "run_fact", out var run));
            Assert.AreEqual(FactHorizon.Run, run.Horizon);
            Assert.IsTrue(registry.TryGetInfo(FactNamespace.World, "meta_fact", out var meta));
            Assert.AreEqual(FactHorizon.Meta, meta.Horizon);
        }

        [Test]
        public void Shape_PermitsMatchesByTokenNotArity()
        {
            var selfHostile = new FactKeyShapeCore(FactNamespace.Actor, "$self", "hostile", FactValueType.Bool);
            var selfWrite = new FactKeyShapeCore(FactNamespace.Actor, "$self", "hostile", FactValueType.Bool);
            var targetWrite = new FactKeyShapeCore(FactNamespace.Actor, "$target", "hostile", FactValueType.Bool);

            Assert.IsTrue(selfHostile.Permits(selfWrite));
            // W4-1: same per-actor arity, different token -> NOT permitted.
            Assert.IsFalse(selfHostile.Permits(targetWrite));
        }

        [Test]
        public void Shape_RejectsTypeMismatch()
        {
            var boolShape = new FactKeyShapeCore(FactNamespace.World, "", "k", FactValueType.Bool);
            var intWrite = new FactKeyShapeCore(FactNamespace.World, "", "k", FactValueType.Int);
            Assert.IsFalse(boolShape.Permits(intWrite));
        }
    }
}
