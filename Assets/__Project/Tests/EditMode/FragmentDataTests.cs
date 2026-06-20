using System.Collections.Generic;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FragmentDataTests
    {
        private static FactEffectCore SetBool(string key) =>
            new FactEffectCore(FactNamespace.World, "", key, FactEffectOp.Set, FactValue.FromBool(true));

        [Test]
        public void QuestData_Footprint_IsUnionOfAllEffectShapes_Deduped()
        {
            var objective = new QuestObjective("o1", "", QuestObjectiveKind.Choice, 1,
                new[] { SetBool("pass_cleared") }); // same shape as onComplete below

            var quest = new QuestData("q", "Q", "", new[] { objective },
                new[] { "errand" },
                onCompleteEffects: new[] { SetBool("pass_cleared") },
                onFailEffects: new[] { SetBool("pass_failed") });

            // pass_cleared appears twice (objective + onComplete) -> deduped; pass_failed once.
            Assert.AreEqual(2, quest.Footprint.Count);
            Assert.Contains(SetBool("pass_cleared").Shape, new List<FactKeyShapeCore>(quest.Footprint));
            Assert.Contains(SetBool("pass_failed").Shape, new List<FactKeyShapeCore>(quest.Footprint));
        }

        [Test]
        public void DialogueData_StartKnot_DefaultsToStart_WhenEmpty()
        {
            var d = new DialogueData("d", "{}", "", null, null, null);
            Assert.AreEqual("start", d.StartKnot);
            Assert.AreEqual(0, d.DeclaredFactWrites.Count);
        }

        [Test]
        public void StoryTemplateData_DerivedFootprint_NullUntilSet_ThenCached()
        {
            var story = new StoryTemplateData("s", null, null, null, null, "road", false);
            Assert.IsNull(story.DerivedFootprint);

            var footprint = new List<FactKeyShapeCore> { SetBool("pass_cleared").Shape };
            story.SetDerivedFootprint(footprint);
            Assert.AreEqual(1, story.DerivedFootprint.Count);
        }

        [Test]
        public void StorySlot_And_Records_AreNullSafe()
        {
            var slot = new StorySlot("slot", SlotKind.Combat, null, true);
            Assert.AreEqual(0, slot.RequiredTags.Count);
            Assert.IsTrue(slot.Optional);

            var quest = new QuestData(null, null, null, null, null, null, null);
            Assert.AreEqual(0, quest.Footprint.Count);
            Assert.AreEqual(string.Empty, quest.QuestId);
        }
    }
}
