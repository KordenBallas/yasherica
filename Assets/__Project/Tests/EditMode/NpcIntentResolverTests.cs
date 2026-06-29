using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Interaction.Core;
using Narrative.Quests.Core;
using Narrative.Stories.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class NpcIntentResolverTests
    {
        private static DialogueData Dlg() =>
            new DialogueData("dlg", "{}", "start", System.Array.Empty<string>(),
                System.Array.Empty<FactKeyShapeCore>(), System.Array.Empty<string>());

        private static QuestData Qst() =>
            new QuestData("qst", "qst", "", System.Array.Empty<QuestObjective>(), System.Array.Empty<string>(),
                System.Array.Empty<FactEffectCore>(), System.Array.Empty<FactEffectCore>());

        private static NpcInstance Actor() => new NpcInstance("npc_01", "arch", "Mara", "villagers");

        private static Casting Cast(QuestData quest, string enemyId) =>
            new Casting(Actor(), Dlg(), quest, enemyId, new ContextBag());

        /// <summary>A story with a dialogue slot plus a combat slot of the given optionality.</summary>
        private static StoryTemplateData StoryWithCombat(bool combatOptional) =>
            new StoryTemplateData("s", new[]
            {
                new StorySlot("d", SlotKind.Dialogue, System.Array.Empty<string>(), false),
                new StorySlot("c", SlotKind.Combat, System.Array.Empty<string>(), combatOptional)
            }, null, null, null, "thread", false);

        private static StoryTemplateData DialogueOnlyStory() =>
            new StoryTemplateData("s", new[] { new StorySlot("d", SlotKind.Dialogue, System.Array.Empty<string>(), false) },
                null, null, null, "thread", false);

        private readonly NpcIntentResolver _resolver = new NpcIntentResolver();

        [Test]
        public void QuestSlotFilled_IsQuestBearer()
        {
            Assert.AreEqual(NpcIntent.QuestBearer, _resolver.Resolve(Cast(Qst(), null), DialogueOnlyStory()));
        }

        [Test]
        public void NoQuestButRequiredCombatSlot_IsHostile()
        {
            Assert.AreEqual(NpcIntent.Hostile, _resolver.Resolve(Cast(null, "enemy_brute"), StoryWithCombat(combatOptional: false)));
        }

        [Test]
        public void NoQuestButOptionalCombatSlot_IsPlain_FightIsADialogueBranch()
        {
            // The raider you can fight OR bribe: an optional combat slot is a dialogue choice, not an
            // approach-aggro. The NPC stays talkable.
            Assert.AreEqual(NpcIntent.Plain, _resolver.Resolve(Cast(null, "enemy_brute"), StoryWithCombat(combatOptional: true)));
        }

        [Test]
        public void NeitherQuestNorCombat_IsPlain()
        {
            Assert.AreEqual(NpcIntent.Plain, _resolver.Resolve(Cast(null, null), DialogueOnlyStory()));
        }

        [Test]
        public void QuestAndRequiredCombat_QuestWins()
        {
            // A quest on offer always reads as quest-bearer, even when combat is also part of the encounter.
            Assert.AreEqual(NpcIntent.QuestBearer, _resolver.Resolve(Cast(Qst(), "enemy_brute"), StoryWithCombat(combatOptional: false)));
        }

        [Test]
        public void NullCasting_IsPlain()
        {
            Assert.AreEqual(NpcIntent.Plain, _resolver.Resolve(null, StoryWithCombat(combatOptional: false)));
        }
    }
}
