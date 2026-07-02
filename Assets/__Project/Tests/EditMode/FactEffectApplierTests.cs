using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FactEffectApplierTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        private FakeLogger _logger;
        private FactStore _store;
        private FactEffectApplier _applier;
        private SubjectContext _context;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            _store = new FactStore(); // permissive store; the applier guards footprint/op-type.
            _applier = new FactEffectApplier(new SubjectResolver(_logger), _logger);
            _context = new SubjectContext().Bind("$self", "npc_07");
        }

        private static FactEffectCore Eff(FactNamespace ns, string token, string key, FactEffectOp op, FactValue val)
            => new FactEffectCore(ns, token, key, op, val);

        private static IReadOnlyCollection<FactKeyShapeCore> Footprint(params FactKeyShapeCore[] shapes) => shapes;

        [Test]
        public void Set_WithinFootprint_Applies()
        {
            var eff = Eff(FactNamespace.World, "", "pass_cleared", FactEffectOp.Set, FactValue.FromBool(true));
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool));

            Assert.IsTrue(_applier.Apply(eff, _store, _context, fp));
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
        }

        [Test]
        public void OutOfFootprint_Rejected_StoreUnchanged_AndWarns()
        {
            var eff = Eff(FactNamespace.World, "", "village_burned", FactEffectOp.Set, FactValue.FromBool(true));
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool));

            Assert.IsFalse(_applier.Apply(eff, _store, _context, fp));
            Assert.IsFalse(_store.Has(FactKey.Global(FactNamespace.World, "village_burned")));
            Assert.AreEqual(1, _logger.Warnings.Count);
        }

        [Test]
        public void Footprint_ComparesByToken_NotArity()
        {
            // W4-1: footprint declares $self; a $target write to the same per-actor key is rejected.
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.Actor, "$self", "hostile", FactValueType.Bool));

            var selfWrite = Eff(FactNamespace.Actor, "$self", "hostile", FactEffectOp.Set, FactValue.FromBool(true));
            var targetWrite = Eff(FactNamespace.Actor, "$target", "hostile", FactEffectOp.Set, FactValue.FromBool(true));

            Assert.IsTrue(_applier.Apply(selfWrite, _store, _context, fp));
            Assert.IsFalse(_applier.Apply(targetWrite, _store, _context, fp));
        }

        [Test]
        public void IllegalOpType_NoOpAndWarns()
        {
            // Add on Bool is illegal.
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.World, "", "flag", FactValueType.Bool));
            var eff = Eff(FactNamespace.World, "", "flag", FactEffectOp.Add, FactValue.FromBool(true));

            Assert.IsFalse(_applier.Apply(eff, _store, _context, fp));
            Assert.AreEqual(1, _logger.Warnings.Count);
        }

        [Test]
        public void Add_OnInt_Accumulates()
        {
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.Faction, "$faction", "reputation", FactValueType.Int));
            var ctx = new SubjectContext().Bind("$faction", "blades");
            var eff = Eff(FactNamespace.Faction, "$faction", "reputation", FactEffectOp.Add, FactValue.FromInt(3));

            _applier.Apply(eff, _store, ctx, fp);
            _applier.Apply(eff, _store, ctx, fp);
            Assert.AreEqual(6, _store.GetOrDefault(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(0)).AsInt());
        }

        [Test]
        public void Toggle_OnBool_Flips()
        {
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.Actor, "$self", "hostile", FactValueType.Bool));
            var eff = Eff(FactNamespace.Actor, "$self", "hostile", FactEffectOp.Toggle, FactValue.FromBool(false));

            _applier.Apply(eff, _store, _context, fp);
            Assert.IsTrue(_store.GetOrDefault(new FactKey(FactNamespace.Actor, "npc_07", "hostile"), FactValue.FromBool(false)).AsBool());
            _applier.Apply(eff, _store, _context, fp);
            Assert.IsFalse(_store.GetOrDefault(new FactKey(FactNamespace.Actor, "npc_07", "hostile"), FactValue.FromBool(false)).AsBool());
        }

        [Test]
        public void RuntimeOverride_SuppliesExactValue_StillFootprintGated()
        {
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.Faction, "$faction", "reputation", FactValueType.Int));
            var ctx = new SubjectContext().Bind("$faction", "blades");
            // Declared value 0, but play-time override writes 42.
            var eff = Eff(FactNamespace.Faction, "$faction", "reputation", FactEffectOp.Set, FactValue.FromInt(0));

            Assert.IsTrue(_applier.Apply(eff, _store, ctx, fp, FactValue.FromInt(42)));
            Assert.AreEqual(42, _store.GetOrDefault(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(0)).AsInt());
        }

        [Test]
        public void QuestEffect_ValidatesAgainstItsOwnFootprint_NoActiveStoryNeeded()
        {
            // W2-1: a detached quest effect carries its own footprint (the effect's own shape).
            var questEffect = Eff(FactNamespace.World, "", "pass_cleared", FactEffectOp.Set, FactValue.FromBool(true));
            var questFootprint = Footprint(questEffect.Shape);

            Assert.IsTrue(_applier.Apply(questEffect, _store, _context, questFootprint));
            Assert.IsTrue(_store.GetOrDefault(FactKey.Global(FactNamespace.World, "pass_cleared"), FactValue.FromBool(false)).AsBool());
        }

        [Test]
        public void Remove_WithinFootprint_ClearsFact()
        {
            var key = FactKey.Global(FactNamespace.World, "pass_cleared");
            _store.Set(key, FactValue.FromBool(true));
            var fp = Footprint(new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool));
            var eff = Eff(FactNamespace.World, "", "pass_cleared", FactEffectOp.Remove, FactValue.FromBool(false));

            Assert.IsTrue(_applier.Apply(eff, _store, _context, fp));
            Assert.IsFalse(_store.Has(key));
        }
    }
}
