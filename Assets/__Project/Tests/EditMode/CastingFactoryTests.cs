using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CastingFactoryTests
    {
        private static DialogueData Dlg(string id, params string[] tags) =>
            new DialogueData(id, "{}", "start", System.Array.Empty<string>(),
                new[] { new FactKeyShapeCore(FactNamespace.World, "", "pass_cleared", FactValueType.Bool) }, tags);

        private static QuestData Qst(string id, params string[] tags) =>
            new QuestData(id, id, "", System.Array.Empty<QuestObjective>(), tags,
                new[] { new FactEffectCore(FactNamespace.World, "", "pass_cleared", FactEffectOp.Set, FactValue.FromBool(true)) },
                System.Array.Empty<FactEffectCore>());

        private static StorySlot Slot(string id, SlotKind kind, bool optional, params string[] required) =>
            new StorySlot(id, kind, required, optional);

        private static NpcInstance Actor() => new NpcInstance("npc_07", "arch", "Razor", "free_blades");

        private CastingFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CastingFactory(new DeterministicRandom(1));
        }

        [Test]
        public void Cast_FillsSlotsByTag_AndInjectsAvailabilityVars()
        {
            var story = new StoryTemplateData("s", new[]
            {
                Slot("d", SlotKind.Dialogue, false, "shakedown"),
                Slot("q", SlotKind.Quest, true, "errand"),
                Slot("c", SlotKind.Combat, true, "bandit")
            }, null, null, null, "road", false);

            var library = new FragmentLibrary(
                new[] { Dlg("dlg", "shakedown") },
                new[] { Qst("qst", "errand") },
                new[] { new EnemyFragment("enemy_brute", new[] { "bandit" }) });

            var casting = _factory.Cast(story, Actor(), library);

            Assert.IsNotNull(casting);
            Assert.AreEqual("dlg", casting.Dialogue.DialogueId);
            Assert.IsTrue(casting.QuestSlotFilled);
            Assert.IsTrue(casting.CombatSlotFilled);
            Assert.AreEqual("enemy_brute", casting.OptionalEnemyId);

            Assert.IsTrue(casting.Context.TryGetVariable("combat_available", out var combat) && (bool)combat);
            Assert.IsTrue(casting.Context.TryGetVariable("quest_available", out var quest) && (bool)quest);
            Assert.IsTrue(casting.Context.TryGetVariable("npc_name", out var name) && (string)name == "Razor");
        }

        [Test]
        public void Cast_OptionalSlotUnfilled_AvailabilityFalse()
        {
            var story = new StoryTemplateData("s", new[]
            {
                Slot("d", SlotKind.Dialogue, false, "shakedown"),
                Slot("c", SlotKind.Combat, true, "bandit")
            }, null, null, null, "road", false);

            // No enemy matching "bandit".
            var library = new FragmentLibrary(new[] { Dlg("dlg", "shakedown") }, null, null);

            var casting = _factory.Cast(story, Actor(), library);
            Assert.IsNotNull(casting);
            Assert.IsFalse(casting.CombatSlotFilled);
            Assert.IsTrue(casting.Context.TryGetVariable("combat_available", out var combat) && !(bool)combat);
        }

        [Test]
        public void Cast_RequiredDialogueSlotUnmatched_ReturnsNull()
        {
            var story = new StoryTemplateData("s", new[] { Slot("d", SlotKind.Dialogue, false, "missing") },
                null, null, null, "road", false);
            var library = new FragmentLibrary(new[] { Dlg("dlg", "shakedown") }, null, null);

            Assert.IsNull(_factory.Cast(story, Actor(), library));
        }

        [Test]
        public void Cast_MultipleMatches_DeterministicSeededPick()
        {
            var story = new StoryTemplateData("s", new[] { Slot("d", SlotKind.Dialogue, false, "shakedown") },
                null, null, null, "road", false);
            var library = new FragmentLibrary(new[] { Dlg("dlg_a", "shakedown"), Dlg("dlg_b", "shakedown") }, null, null);

            var first = new CastingFactory(new DeterministicRandom(42)).Cast(story, Actor(), library);
            var second = new CastingFactory(new DeterministicRandom(42)).Cast(story, Actor(), library);

            Assert.AreEqual(first.Dialogue.DialogueId, second.Dialogue.DialogueId);
        }

        [Test]
        public void DeriveFootprint_IsUnionOfCandidateFragmentFootprints()
        {
            var story = new StoryTemplateData("s", new[]
            {
                Slot("d", SlotKind.Dialogue, false, "shakedown"),
                Slot("q", SlotKind.Quest, true, "errand")
            }, null, null, null, "road", false);

            var library = new FragmentLibrary(
                new[] { Dlg("dlg", "shakedown") }, // writes pass_cleared
                new[] { Qst("qst", "errand") },    // footprint pass_cleared
                null);

            var footprint = _factory.DeriveFootprint(story, library);
            Assert.AreEqual(1, footprint.Count); // pass_cleared shared & deduped
            Assert.AreSame(footprint, story.DerivedFootprint);
        }
    }
}
