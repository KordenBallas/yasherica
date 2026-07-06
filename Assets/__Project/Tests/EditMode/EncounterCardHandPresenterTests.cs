using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Core;
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
    /// Drives a real <see cref="DialogueRunner"/> (over the Ink-free <see cref="FakeStoryManager"/>)
    /// through <see cref="EncounterCardHandPresenter"/> with a stub view, asserting the composed card hand
    /// and the pick routing. Pure C# — no UnityEngine.
    /// </summary>
    [TestFixture]
    public class EncounterCardHandPresenterTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        private sealed class StubCardHandView : IEncounterCardHandView
        {
            public event Action<int> OnCardSelected;
            public event Action OnRevealCompleted;
            public event Action OnContinueRequested;

            public string Speaker;
            public string Portrait;
            public string Situation;
            public bool Visible;
            public IReadOnlyList<EncounterCardViewData> LastCards = Array.Empty<EncounterCardViewData>();

            public void SetSpeaker(string name) => Speaker = name;
            public void SetPortrait(string archetypeId) => Portrait = archetypeId;
            public void ShowSituation(string line) => Situation = line;
            public void ShowCards(IReadOnlyList<EncounterCardViewData> cards) => LastCards = cards;
            public void SetVisible(bool visible) => Visible = visible;

            public void FireCard(int index) => OnCardSelected?.Invoke(index);
            public void FireRevealCompleted() => OnRevealCompleted?.Invoke(); // the line finished typing
            public void FireContinue() => OnContinueRequested?.Invoke();       // a tap on a fully-shown line
        }

        private FakeLogger _logger;
        private FactStore _store;
        private FactEffectApplier _applier;
        private DialogueTagParser _parser;
        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private StubCardHandView _view;
        private EncounterCardHandPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            var registry = new FactKeyRegistry(Array.Empty<FactKeyInfo>());
            _store = new FactStore(registry, _logger);
            var resolver = new SubjectResolver(_logger);
            _applier = new FactEffectApplier(resolver, _logger);
            _parser = new DialogueTagParser(registry, _logger);
            _fake = new FakeStoryManager();
            var session = new DialogueSession(_fake);
            _runner = new DialogueRunner(session, _store, _applier, _parser, recorder: null, logger: _logger);
            _view = new StubCardHandView();
            _presenter = new EncounterCardHandPresenter(_runner, _view, _logger);
            _presenter.Initialize();
        }

        private static DialogueData Dialogue() => new DialogueData(
            "dlg_demo", "{}", "start", Array.Empty<string>(),
            Array.Empty<FactKeyShapeCore>(), Array.Empty<string>());

        private static Casting Cast(string enemyId, QuestData quest = null)
        {
            var actor = new NpcInstance("npc_01", "arch_villager", "Aldo", "free_folk");
            var ctx = new ContextBag().BindSubject("$self", "npc_01");
            return new Casting(actor, Dialogue(), quest, enemyId, ctx);
        }

        private static EncounterCardType[] Types(IReadOnlyList<EncounterCardViewData> cards) =>
            cards.Select(c => c.CardType).ToArray();

        [Test]
        public void NarrationLine_ShowsNoCards_ThenRevealAutoAdvancesToChoices()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Raiders cleaned out my barn."),
                FakeStoryManager.Frame.ChoicePoint(
                    new[] { new StoryChoice(0, "I'll bring your grain back.") },
                    new[] { new[] { FakeStoryManager.Frame.Line("The gods walk with you.") } }));

            _runner.Begin(Cast(enemyId: null)); // narration line first
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("Raiders cleaned out my barn.", _view.Situation);
            Assert.IsEmpty(_view.LastCards); // no cards while a line types

            _view.FireRevealCompleted(); // the line finished revealing -> auto-advance into the choices
            Assert.AreEqual(new[] { EncounterCardType.QuestOffer, EncounterCardType.Leave }, Types(_view.LastCards));
            Assert.AreEqual("I'll bring your grain back.", _view.LastCards[0].Label);
        }

        [Test]
        public void PickQuestOffer_ShowsClosingReply_DismissTapClosesTheBox()
        {
            _fake.Script(
                FakeStoryManager.Frame.Line("Raiders cleaned out my barn."),
                FakeStoryManager.Frame.ChoicePoint(
                    new[] { new StoryChoice(0, "I'll bring your grain back.") },
                    new[] { new[] { FakeStoryManager.Frame.Line("The gods walk with you.") } }));

            string ended = null;
            _runner.OnDialogueEnded += o => ended = o;

            _runner.Begin(Cast(enemyId: null));
            _view.FireRevealCompleted(); // line -> choices

            _view.FireCard(0); // pick the quest offer -> its closing reply is shown
            Assert.AreEqual("The gods walk with you.", _view.Situation);
            Assert.IsEmpty(_view.LastCards); // no cards while the closing reply types
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);
            Assert.IsTrue(_view.Visible);

            _view.FireRevealCompleted(); // a closing reply does NOT auto-advance; it waits for the tap
            Assert.AreEqual(DialogueRunnerState.AwaitingContinue, _runner.State);
            Assert.IsTrue(_view.Visible);

            _view.FireContinue(); // the dismiss tap closes the box
            Assert.AreEqual("exit", ended);
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void EncounterChrome_SeedsPortraitAndNpcName_FromCasting()
        {
            _fake.Script(FakeStoryManager.Frame.Line("Raiders cleaned out my barn."));

            _runner.Begin(Cast(enemyId: null)); // no #speaker tag in the script
            Assert.AreEqual("arch_villager", _view.Portrait); // archetype id forwarded for portrait resolution
            Assert.AreEqual("Aldo", _view.Speaker); // falls back to the NPC's display name
        }

        [Test]
        public void QuestOfferCard_CarriesOfferedQuestTitleAndSummary()
        {
            var quest = new QuestData("q_bounty", "Farmer's Bounty", "Recover the stolen grain.",
                Array.Empty<QuestObjective>(), Array.Empty<string>(),
                Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>());

            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[] { new StoryChoice(0, "I'll bring your grain back.") },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0] }));

            _runner.Begin(Cast(enemyId: null, quest: quest));

            var questCard = _view.LastCards[0];
            Assert.AreEqual(EncounterCardType.QuestOffer, questCard.CardType);
            Assert.AreEqual("Farmer's Bounty", questCard.QuestTitle);
            Assert.AreEqual("Recover the stolen grain.", questCard.QuestObjective);
            Assert.AreEqual("I'll bring your grain back.", questCard.Label); // reply text preserved as fallback

            // The persistent Leave card carries no quest job.
            var leaveCard = _view.LastCards[_view.LastCards.Count - 1];
            Assert.AreEqual(EncounterCardType.Leave, leaveCard.CardType);
            Assert.IsNull(leaveCard.QuestTitle);
        }

        [Test]
        public void TaggedAttackChoice_BecomesAttackCard_NoSystemAttack()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[]
                {
                    new StoryChoice(0, "Drop the grain.", new[] { "card: attack" }),
                    new StoryChoice(1, "Take a cut, look away.")
                },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0], new Tests.EditMode.FakeStoryManager.Frame[0] }));

            _runner.Begin(Cast(enemyId: "7")); // combat-capable
            Assert.AreEqual(
                new[] { EncounterCardType.Attack, EncounterCardType.QuestOffer, EncounterCardType.Leave },
                Types(_view.LastCards)); // tagged attack present => no extra system attack
        }

        [Test]
        public void CombatCapable_UntaggedChoices_AddsSystemAttackAtDecision()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[] { new StoryChoice(0, "Parley.") },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0] }));

            _runner.Begin(Cast(enemyId: "7")); // combat-capable, no tagged attack
            Assert.AreEqual(
                new[] { EncounterCardType.QuestOffer, EncounterCardType.Attack, EncounterCardType.Leave },
                Types(_view.LastCards));
        }

        [Test]
        public void PickQuestOffer_SelectsThatInkChoice()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[]
                {
                    new StoryChoice(0, "First."),
                    new StoryChoice(1, "Second.")
                },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0], new Tests.EditMode.FakeStoryManager.Frame[0] }));

            _runner.Begin(Cast(enemyId: null));
            // hand = [QuestOffer First(0), QuestOffer Second(1), Leave]; pick the second card.
            _view.FireCard(1);
            Assert.AreEqual(1, _fake.LastChoiceIndex);
        }

        [Test]
        public void PickLeave_EndsTheEncounter()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[] { new StoryChoice(0, "Talk.") },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0] }));

            string ended = null;
            _runner.OnDialogueEnded += o => ended = o;

            _runner.Begin(Cast(enemyId: null));
            // hand = [QuestOffer Talk, Leave]; the Leave card is the last index.
            _view.FireCard(_view.LastCards.Count - 1);

            Assert.AreEqual("leave", ended);
            Assert.AreEqual(DialogueRunnerState.Ended, _runner.State);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void PickSystemAttack_TriggersCombat()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[] { new StoryChoice(0, "Parley.") },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0] }));

            string enemy = null;
            CombatInitiator? initiator = null;
            _runner.OnCombatTriggered += (e, who) => { enemy = e; initiator = who; };

            _runner.Begin(Cast(enemyId: "7")); // hand = [QuestOffer Parley, Attack(system), Leave]
            _view.FireCard(1); // the system Attack card

            Assert.AreEqual("7", enemy);
            Assert.AreEqual(CombatInitiator.Player, initiator); // the player chose to attack (D2)
            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);
        }

        [Test]
        public void PickTaggedAttack_SelectsTheInkChoice_NotSystemCombat()
        {
            _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                new[]
                {
                    new StoryChoice(0, "Drop the grain.", new[] { "card: attack" }),
                    new StoryChoice(1, "Take a cut.")
                },
                new[] { new Tests.EditMode.FakeStoryManager.Frame[0], new Tests.EditMode.FakeStoryManager.Frame[0] }));

            bool combatRaised = false;
            _runner.OnCombatTriggered += (_, __) => combatRaised = true;

            _runner.Begin(Cast(enemyId: "7"));
            _view.FireCard(0); // the tagged Attack card -> selects Ink choice 0, NOT TriggerCombat

            Assert.AreEqual(0, _fake.LastChoiceIndex);
            Assert.IsFalse(combatRaised); // routed through Ink (its start-combat would fire later)
        }
    }
}
