using System;
using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Encounter;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Shape A — several quest offers on one NPC at one moment (P1-9,
    /// multiple-and-competing-offers.md): a casting carries several quest slots, the Ink choices are
    /// tagged <c>offer-quest: &lt;tag&gt;</c>, the hand labels each card with ITS quest (same tier,
    /// different belonging), and picking one mints only that quest.
    /// </summary>
    [TestFixture]
    public class MultipleOffersTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class StubCardHandView : IEncounterCardHandView
        {
            public event Action<int> OnCardSelected;
            public event Action OnRevealCompleted;
            public event Action OnContinueRequested;

            public IReadOnlyList<EncounterCardViewData> LastCards = Array.Empty<EncounterCardViewData>();

            public void SetSpeaker(string name) { }
            public void SetPortrait(string archetypeId) { }
            public void ShowSituation(string line) { }
            public void ShowCards(IReadOnlyList<EncounterCardViewData> cards) => LastCards = cards;
            public void SetVisible(bool visible) { }

            public void FireCard(int index) => OnCardSelected?.Invoke(index);
            public void FireRevealCompleted() => OnRevealCompleted?.Invoke();
            public void FireContinue() => OnContinueRequested?.Invoke();
        }

        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private LiveQuestRegistry _registry;
        private StubCardHandView _view;
        private EncounterCardHandPresenter _presenter;

        private static readonly QuestData Errand = new QuestData(
            "qst_errand", "Поручение", "Отнеси свёрток.", Array.Empty<QuestObjective>(),
            new[] { "frog-errand" }, Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>(),
            new[] { new QuestRewardCore(1, "utility", QuestRewardPayloadKind.Artifact) });

        private static readonly QuestData Guard = new QuestData(
            "qst_guard", "Стража плёса", "Прогони цапель.", Array.Empty<QuestObjective>(),
            new[] { "frog-guard" }, Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>(),
            new[] { new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact) });

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var factRegistry = new FactKeyRegistry(Array.Empty<FactKeyInfo>());
            var store = new FactStore(factRegistry, logger);
            var applier = new FactEffectApplier(new SubjectResolver(logger), logger);
            var parser = new DialogueTagParser(factRegistry, logger);
            _fake = new FakeStoryManager();
            _registry = new LiveQuestRegistry();
            _runner = new DialogueRunner(new DialogueSession(_fake), store, applier, parser,
                recorder: null, questRegistry: _registry, logger: logger);
            _view = new StubCardHandView();
            _presenter = new EncounterCardHandPresenter(_runner, _view, logger);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private Casting TwoOfferCasting()
        {
            var actor = new NpcInstance("npc_elder", "arch_frog", "Elder", "frogfolk");
            var dialogue = new DialogueData("dlg_two", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), new[] { "frog-elder-open" });
            return Casting.WithQuests(actor, dialogue, new[] { Errand, Guard }, null, new ContextBag(),
                "story_frog", "frog_marsh");
        }

        private void ScriptTwoOfferEncounter()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Две беды у болота."),
                FakeStoryManager.Frame.ChoicePoint(
                    new[]
                    {
                        new StoryChoice(0, "Отнести свёрток.", new[] { "offer-quest: frog-errand" }),
                        new StoryChoice(1, "Прогнать цапель.", new[] { "offer-quest: frog-guard" })
                    },
                    new[]
                    {
                        new[] { FakeStoryManager.Frame.Line("Беги шустро.", "offer-quest: frog-errand") },
                        new[] { FakeStoryManager.Frame.Line("Напугай их.", "offer-quest: frog-guard") }
                    }));
        }

        [Test]
        public void HandShowsBothOffers_EachLabelledWithItsOwnQuest()
        {
            ScriptTwoOfferEncounter();
            _runner.Begin(TwoOfferCasting());
            _view.FireRevealCompleted(); // line -> choices

            var offers = _view.LastCards.Where(c => c.CardType == EncounterCardType.QuestOffer).ToList();
            Assert.AreEqual(2, offers.Count, "both resolutions are shown together in the hand");
            Assert.AreEqual("Поручение", offers[0].QuestTitle);
            Assert.AreEqual("Стража плёса", offers[1].QuestTitle);
        }

        [Test]
        public void BothOffers_TelegraphSameTier_DifferentBelonging()
        {
            ScriptTwoOfferEncounter();
            _runner.Begin(TwoOfferCasting());
            _view.FireRevealCompleted();

            var offers = _view.LastCards.Where(c => c.CardType == EncounterCardType.QuestOffer).ToList();
            Assert.IsTrue(offers.All(c => c.HasRewardTelegraph));
            Assert.AreEqual(offers[0].RewardTier, offers[1].RewardTier,
                "same tier - the choice may never read as bigger loot");
            Assert.AreNotEqual(offers[0].BelongingId, offers[1].BelongingId,
                "different belonging - the choice reads as which currency");
        }

        [Test]
        public void PickingSecondOffer_MintsOnlyThatQuest()
        {
            ScriptTwoOfferEncounter();
            _runner.Begin(TwoOfferCasting());
            _view.FireRevealCompleted();

            var guardCard = _view.LastCards.First(c => c.QuestTitle == "Стража плёса");
            _view.FireCard(guardCard.Index);

            Assert.AreEqual("qst_guard", _runner.ActiveQuest.Data.QuestId);
            Assert.IsTrue(_registry.TryGet("qst_guard", out _), "the picked resolution is live");
            Assert.IsFalse(_registry.TryGet("qst_errand", out _),
                "the alternative was not also taken (one trouble, one resolution)");
        }

        [Test]
        public void OfferQuestTag_WithUnknownTag_FailsClosed()
        {
            _fake.Script(FakeStoryManager.Frame.Line("...", "offer-quest: no-such-quest"));
            _runner.Begin(TwoOfferCasting());

            Assert.IsNull(_runner.ActiveQuest);
            var accepted = _fake.SetVarCalls.Last(kv => kv.Key == DialogueRunner.QuestAcceptedVariable);
            Assert.AreEqual(false, accepted.Value);
        }

        [Test]
        public void LegacySingleOffer_UntaggedChoice_FallsBackToFirstQuest()
        {
            var actor = new NpcInstance("npc_farmer", "arch_villager", "Aldo", "free_folk");
            var dialogue = new DialogueData("dlg_one", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), Array.Empty<string>());
            var casting = new Casting(actor, dialogue, Errand, null, new ContextBag());

            _fake.Script(
                FakeStoryManager.Frame.Line("Беда одна."),
                FakeStoryManager.Frame.ChoicePoint(
                    new[] { new StoryChoice(0, "Помогу.") },
                    new[] { new[] { FakeStoryManager.Frame.Line("Спасибо.", "offer-quest:") } }));

            _runner.Begin(casting);
            _view.FireRevealCompleted();

            var offer = _view.LastCards.First(c => c.CardType == EncounterCardType.QuestOffer);
            Assert.AreEqual("Поручение", offer.QuestTitle, "untagged choice labels with the single offer");

            _view.FireCard(offer.Index);
            Assert.AreEqual("qst_errand", _runner.ActiveQuest.Data.QuestId);
        }

        [Test]
        public void RevisitedCasting_RestoresThePickedQuest_NotTheFirstSlot()
        {
            ScriptTwoOfferEncounter();
            _runner.Begin(TwoOfferCasting());
            _view.FireRevealCompleted();
            _view.FireCard(_view.LastCards.First(c => c.QuestTitle == "Стража плёса").Index);
            _view.FireRevealCompleted();
            _view.FireContinue(); // close the encounter

            // A later dialogue on the same actor restores the in-flight guard quest (continuity),
            // even though the errand sits in the first slot.
            _fake.Script(FakeStoryManager.Frame.Line("Ну как цапли?"));
            _runner.Begin(TwoOfferCasting());

            Assert.AreEqual("qst_guard", _runner.ActiveQuest.Data.QuestId);
        }
    }
}
