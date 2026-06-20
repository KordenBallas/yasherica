using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DialogueSessionAndCastingTests
    {
        private static DialogueData Dialogue() => new DialogueData(
            "dlg", "{ink}", "start",
            new[] { "npc_name", "combat_available" },
            new[] { new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool) },
            new[] { "shakedown" });

        [Test]
        public void StartFresh_InjectsDeclaredVariablesFromContext()
        {
            var fake = new FakeStoryManager();
            var session = new DialogueSession(fake);
            var context = new ContextBag().SetVariable("npc_name", "Razor").SetVariable("combat_available", true);

            session.StartFresh(Dialogue(), context);

            Assert.AreEqual("{ink}", fake.LoadedJson);
            Assert.AreEqual("Razor", fake.GetVariable("npc_name"));
            Assert.AreEqual(true, fake.GetVariable("combat_available"));
            Assert.AreEqual(1, session.Footprint.Count);
        }

        [Test]
        public void Restore_DoesNotReinjectVariables_TrustsInkState()
        {
            // W2-4: a mid-session reload must keep play-time values, not casting defaults.
            var fake = new FakeStoryManager();
            var session = new DialogueSession(fake);
            var context = new ContextBag().SetVariable("npc_name", "Razor");

            session.Restore(Dialogue(), "saved-ink-state");

            Assert.AreEqual("saved-ink-state", fake.LoadedState);
            Assert.AreEqual(0, fake.SetVarCalls.Count); // nothing re-injected
        }

        [Test]
        public void StartFresh_OnlyInjectsDeclaredVariablesPresentInContext()
        {
            var fake = new FakeStoryManager();
            var session = new DialogueSession(fake);
            var context = new ContextBag().SetVariable("npc_name", "Razor"); // combat_available absent

            session.StartFresh(Dialogue(), context);

            Assert.AreEqual(1, fake.SetVarCalls.Count);
            Assert.AreEqual("npc_name", fake.SetVarCalls[0].Key);
        }

        [Test]
        public void Casting_DerivesCombatAllowedFromFilledSlot_NoStoredFlag()
        {
            var actor = new NpcInstance("npc_07", "arch_road_bandit", "Razor", "free_blades");
            var withCombat = new Casting(actor, Dialogue(), null, "enemy_bandit_brute", new ContextBag());
            var withoutCombat = new Casting(actor, Dialogue(), null, null, new ContextBag());

            Assert.IsTrue(withCombat.CombatAllowed);
            Assert.IsTrue(withCombat.CombatSlotFilled);
            Assert.IsFalse(withoutCombat.CombatAllowed);
            Assert.IsFalse(withoutCombat.QuestSlotFilled);
        }

        [Test]
        public void ContextBag_ResolvesSubjectTokens()
        {
            var bag = new ContextBag().BindSubject("$self", "npc_07").BindSubject("$faction", "free_blades");
            Assert.IsTrue(bag.TryGet("$self", out var self));
            Assert.AreEqual("npc_07", self);
            Assert.IsTrue(bag.TryGet("$faction", out var faction));
            Assert.AreEqual("free_blades", faction);
            Assert.IsFalse(bag.TryGet("$missing", out _));
        }
    }
}
