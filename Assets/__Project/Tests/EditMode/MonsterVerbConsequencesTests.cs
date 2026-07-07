using System;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The Monster verb's consequences (P1-7, attack-card-monster-verb.md): a talkable NPC dying in a
    /// dialogue-routed fight writes the slain/Conquest facts and forecloses the encounter's thread —
    /// identically for both entry points (player Attack card, NPC self-initiation), and never on a loss.
    /// </summary>
    [TestFixture]
    public class MonsterVerbConsequencesTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private FakeStoryManager _fake;
        private DialogueRunner _runner;
        private FactStore _store;
        private ThreadLedger _threads;
        private MonsterVerbConsequences _consequences;

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "path_conquest", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
                new FactKeyInfo(FactNamespace.Actor, "slain", FactScope.PerActor, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "thread_retired", FactScope.PerThread, FactValueType.String, FactValue.FromString(""))
            });
            _store = new FactStore(registry, logger);
            var applier = new FactEffectApplier(new SubjectResolver(logger), logger);
            var parser = new DialogueTagParser(registry, logger);
            _fake = new FakeStoryManager();
            _runner = new DialogueRunner(new DialogueSession(_fake), _store, applier, parser, logger: logger);
            _threads = new ThreadLedger();
            _consequences = new MonsterVerbConsequences(_runner, _store, _threads, logger);
            _consequences.Initialize();
        }

        [TearDown]
        public void TearDown() => _consequences.Dispose();

        private Casting HostileCasting(string threadId = "raider_pact")
        {
            var actor = new NpcInstance("npc_raider", "arch_raider", "Gnash", "free_blades");
            var dialogue = new DialogueData("dlg", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), Array.Empty<string>());
            return new Casting(actor, dialogue, (Narrative.Quests.Core.QuestData)null, "enemy_raider",
                new ContextBag(), "story_raider", threadId);
        }

        private void BeginSuspendedFight(bool playerPicked)
        {
            _threads.Open("raider_pact", ThreadKind.Ephemeral, 0);
            if (playerPicked)
            {
                // The player's Attack card: system TriggerCombat (no Ink branch needed).
                _fake.Script(FakeStoryManager.Frame.ChoicePoint(
                    new[] { new StoryChoice(0, "talk") },
                    new[] { new[] { FakeStoryManager.Frame.Line("...") } }));
                _runner.Begin(HostileCasting());
                _runner.TriggerCombat();
            }
            else
            {
                // NPC self-initiation: a start-combat tag fires without any player pick.
                _fake.Script(FakeStoryManager.Frame.Line("Он бросается первым.", "start-combat: enemy_raider"));
                _runner.Begin(HostileCasting());
            }

            Assert.AreEqual(DialogueRunnerState.AwaitingExternal, _runner.State);
        }

        [Test]
        public void Win_WritesSlainFact_AndConquestLean()
        {
            BeginSuspendedFight(playerPicked: true);
            _runner.ReportCombatResult(true);

            Assert.IsTrue(_store.GetBool(ActorFacts.Slain, "npc_raider"));
            Assert.AreEqual(1, _store.GetInt(WorldFacts.PathConquest));
        }

        [Test]
        public void Win_ForeclosesTheEncounterThread_WithIndicatorFact()
        {
            BeginSuspendedFight(playerPicked: true);
            _runner.ReportCombatResult(true);

            Assert.IsTrue(_threads.TryGet("raider_pact", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Foreclosed, record.Reason);

            var indicator = _store.GetOrDefault(
                new FactKey(FactNamespace.World, "raider_pact", ThreadFactKeys.Retired),
                FactValue.FromString(""));
            Assert.AreEqual(ThreadFactKeys.RetiredValueForeclosed, indicator.AsString());
        }

        [Test]
        public void SelfInitiatedFight_HasTheSameConsequences()
        {
            BeginSuspendedFight(playerPicked: false);
            _runner.ReportCombatResult(true);

            Assert.IsTrue(_store.GetBool(ActorFacts.Slain, "npc_raider"));
            Assert.AreEqual(1, _store.GetInt(WorldFacts.PathConquest));
            Assert.IsTrue(_threads.TryGet("raider_pact", out var record));
            Assert.AreEqual(ThreadRetirementReason.Foreclosed, record.Reason);
        }

        [Test]
        public void Loss_WritesNothing_AndLeavesTheThreadLive()
        {
            BeginSuspendedFight(playerPicked: true);
            _runner.ReportCombatResult(false);

            Assert.IsFalse(_store.GetBool(ActorFacts.Slain, "npc_raider"));
            Assert.AreEqual(0, _store.GetInt(WorldFacts.PathConquest));
            Assert.IsTrue(_threads.TryGet("raider_pact", out var record));
            Assert.AreEqual(ThreadState.Live, record.State);
        }

        [Test]
        public void ThreadlessEncounter_StillWritesActorAndPathFacts()
        {
            _fake.Script(FakeStoryManager.Frame.Line("Засада!", "start-combat: enemy_raider"));
            var actor = new NpcInstance("npc_ambusher", "arch_raider", "Snarl", "free_blades");
            var dialogue = new DialogueData("dlg", "{}", "start", Array.Empty<string>(),
                Array.Empty<FactKeyShapeCore>(), Array.Empty<string>());
            _runner.Begin(new Casting(actor, dialogue, (Narrative.Quests.Core.QuestData)null,
                "enemy_raider", new ContextBag()));

            _runner.ReportCombatResult(true);

            Assert.IsTrue(_store.GetBool(ActorFacts.Slain, "npc_ambusher"));
            Assert.AreEqual(1, _store.GetInt(WorldFacts.PathConquest));
        }
    }
}
