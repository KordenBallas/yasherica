using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Barks.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Encounter;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Shape B — the competing, mutually-exclusive fork (P1-8,
    /// multiple-and-competing-offers.md): two offers on opposed threads, separated in time;
    /// committing to either writes the fact that contradicts the other thread's premise, so the
    /// opposing thread fails on conflict via the SHIPPED maintenance mechanism (indicator only, no
    /// closing beat). Mirrors the authored barn_raid / raider_pact demo predicates. Also covers the
    /// dark-offer bark slot firing when the Monster-lean offer is presented.
    /// </summary>
    [TestFixture]
    public class CompetingOffersForkTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private FactStore _store;
        private ThreadLedger _ledger;
        private ThreadMaintenanceService _maintenance;

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "raider_offer_taken", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "grain_recovered", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "thread_retired", FactScope.PerThread, FactValueType.String, FactValue.FromString(""))
            });
            _store = new FactStore(registry, logger);
            _ledger = new ThreadLedger();

            // The authored fork shape: each side's premise is the other side's commit fact held false.
            var farmerSide = new ThreadDefinitionData("barn_raid", ThreadKind.Ephemeral,
                new[] { WorldFalse("raider_offer_taken") }, Array.Empty<FactPredicate>(), 4);
            var raiderSide = new ThreadDefinitionData("raider_pact", ThreadKind.Ephemeral,
                new[] { WorldFalse("grain_recovered") }, Array.Empty<FactPredicate>(), 4);
            var catalog = new ThreadCatalog(new[] { farmerSide, raiderSide }, 4);
            _maintenance = new ThreadMaintenanceService(_ledger, catalog,
                new PreconditionEvaluator(new SubjectResolver(logger), registry, logger), logger);
        }

        private static FactPredicate WorldFalse(string key) =>
            new FactPredicate(FactNamespace.World, "", key, ComparisonOp.Eq, FactValue.FromBool(false));

        [Test]
        public void CommittingToTheRaider_FailsTheFarmerThread_OnConflict()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);      // took the farmer's bounty
            _ledger.Open("raider_pact", ThreadKind.Ephemeral, 2);    // his counter-offer arrived later

            _store.Set(FactKey.Global(FactNamespace.World, "raider_offer_taken"), FactValue.FromBool(true));
            _maintenance.Tick(3, _store);

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var farmer));
            Assert.AreEqual(ThreadState.Failed, farmer.State);
            Assert.AreEqual(ThreadRetirementReason.Conflict, farmer.Reason,
                "committing to B makes A's premise impossible - fail on fact-conflict, no closing beat");

            Assert.IsTrue(_ledger.TryGet("raider_pact", out var raider));
            Assert.AreEqual(ThreadState.Live, raider.State, "the taken side stays live");
        }

        [Test]
        public void HonoringTheBounty_FailsTheRaiderThread_OnConflict()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _ledger.Open("raider_pact", ThreadKind.Ephemeral, 2);

            _store.Set(FactKey.Global(FactNamespace.World, "grain_recovered"), FactValue.FromBool(true));
            _maintenance.Tick(3, _store);

            Assert.IsTrue(_ledger.TryGet("raider_pact", out var raider));
            Assert.AreEqual(ThreadState.Failed, raider.State);
            Assert.AreEqual(ThreadRetirementReason.Conflict, raider.Reason);

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var farmer));
            Assert.AreEqual(ThreadState.Live, farmer.State);
        }

        [Test]
        public void TheTwoSides_AreNeverBothCompletable()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _ledger.Open("raider_pact", ThreadKind.Ephemeral, 2);

            // Whatever order the commits land in, at most one side survives.
            _store.Set(FactKey.Global(FactNamespace.World, "raider_offer_taken"), FactValue.FromBool(true));
            _store.Set(FactKey.Global(FactNamespace.World, "grain_recovered"), FactValue.FromBool(true));
            _maintenance.Tick(3, _store);

            _ledger.TryGet("barn_raid", out var farmer);
            _ledger.TryGet("raider_pact", out var raider);
            Assert.IsTrue(farmer.State == ThreadState.Failed && raider.State == ThreadState.Failed,
                "opposed commit facts can never leave both arcs alive");
        }

        // ---- the dark-offer bark slot (P1-10 hook on the fork's Monster-lean side) ----

        private sealed class StubHandView : IEncounterCardHandView
        {
            public event Action<int> OnCardSelected;
            public event Action OnRevealCompleted;
            public event Action OnContinueRequested;
            public void SetSpeaker(string name) { }
            public void SetPortrait(string archetypeId) { }
            public void ShowSituation(string line) { }
            public void ShowCards(IReadOnlyList<EncounterCardViewData> cards) { }
            public void SetVisible(bool visible) { }
            public void FireRevealCompleted() => OnRevealCompleted?.Invoke();
        }

        [Test]
        public void PresentingTheDarkOffer_FiresTheDarkOfferBarkSlot_OncePerEncounter()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(Array.Empty<FactKeyInfo>());
            var store = new FactStore(registry, logger);
            var fake = new FakeStoryManager();
            var runner = new DialogueRunner(new DialogueSession(fake), store,
                new FactEffectApplier(new SubjectResolver(logger), logger),
                new DialogueTagParser(registry, logger), logger: logger);

            var barkStore = new FactStore(new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "path_conquest", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
                new FactKeyInfo(FactNamespace.World, "path_restraint", FactScope.Global, FactValueType.Int, FactValue.FromInt(0))
            }), logger);
            var lines = new CauldronBarkLines(
                new Dictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>>
                {
                    { (CauldronBarkSlot.DarkOffer, BarkLean.Restrained), new[] { "devour him" } }
                },
                new[] { "power" });
            var barks = new CauldronBarkService(lines, barkStore, runSeed: 3);
            int fired = 0;
            barks.OnBark += _ => fired++;

            var view = new StubHandView();
            var presenter = new EncounterCardHandPresenter(runner, view, logger, barks);
            presenter.Initialize();

            var darkQuest = new QuestData("qst_pact", "Пакт", "Отнеси мешок.", Array.Empty<QuestObjective>(),
                new[] { "raider-run" }, Array.Empty<FactEffectCore>(), Array.Empty<FactEffectCore>(),
                new[] { new QuestRewardCore(1, "power", QuestRewardPayloadKind.Artifact) });
            var actor = new NpcInstance("npc_raider", "arch_raider", "Gnash", "free_blades");
            var dialogue = new DialogueData("dlg", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), Array.Empty<string>());

            fake.Script(
                FakeStoryManager.Frame.Line("Опять ты."),
                FakeStoryManager.Frame.ChoicePoint(
                    new[] { new StoryChoice(0, "Взять его работу.", new[] { "offer-quest: raider-run" }) },
                    new[] { new[] { FakeStoryManager.Frame.Line("Умно.", "offer-quest: raider-run") } }));
            runner.Begin(new Casting(actor, dialogue, darkQuest, null, new ContextBag(), "s", "raider_pact"));
            view.FireRevealCompleted(); // line -> the hand with the dark offer

            Assert.AreEqual(1, fired, "the cauldron leans in when the Monster-lean offer is presented");
            presenter.Dispose();
        }
    }
}
