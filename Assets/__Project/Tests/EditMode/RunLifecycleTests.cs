using System.Collections.Generic;
using Combat.Core;
using Combat.Integration;
using Core.Persistence;
using Core.SceneFlow;
using Narrative.Dialogue;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// P2-2 lifecycle wiring: death consumes the run save AFTER the world memory flushes (FR2 with
    /// the D8 ordering guarantee), victories touch nothing, and the autosave honors the W3-1
    /// savepoint guard.
    /// </summary>
    [TestFixture]
    public class RunLifecycleTests
    {
        private sealed class RecordingStores : IRunSaveStore, IMetaMemoryFlush
        {
            public readonly List<string> Calls = new List<string>();
            public RunSaveSnapshot Saved;

            public bool Exists() => Saved != null;
            public bool TryLoad(out RunSaveSnapshot snapshot) { snapshot = Saved; return Saved != null; }
            public void Save(RunSaveSnapshot snapshot) { Saved = snapshot; Calls.Add("run-save"); }
            public void Delete() { Saved = null; Calls.Add("run-delete"); }
            public void Flush() => Calls.Add("meta-flush");
        }

        private sealed class RecordingLoader : ISceneLoader
        {
            private readonly List<string> _calls;
            public RecordingLoader(List<string> calls) => _calls = calls;
            public void Load(string sceneName) => _calls.Add("load:" + sceneName);
        }

        private sealed class RecordingArrival : IHubArrivalStore
        {
            private readonly List<string> _calls;
            public RecordingArrival(List<string> calls) => _calls = calls;
            public void MarkDeathReturn() => _calls.Add("mark-death-return");
            public bool TryConsumeDeathReturn() => false;
        }

        private sealed class FakeRunState : IRunStateService
        {
            public bool CaptureAllowed = true;

            public bool TryCaptureAll(DialogueRunnerState dialogueState, out RunSaveSnapshot snapshot)
            {
                bool allowed = CaptureAllowed && dialogueState != DialogueRunnerState.AwaitingExternal;
                snapshot = allowed ? new RunSaveSnapshot() : null;
                return allowed;
            }

            public void RestoreAll(RunSaveSnapshot snapshot) { }
        }

        [Test]
        public void Defeat_FlushesMetaBeforeConsumingTheRunSave()
        {
            var stores = new RecordingStores { Saved = new RunSaveSnapshot() };
            var relay = new CombatOutcomeRelay();
            var lifecycle = new RunLifecycleService(relay, stores, stores);
            lifecycle.Initialize();

            relay.Notify(CombatPhase.Defeat);

            CollectionAssert.AreEqual(new[] { "meta-flush", "run-delete" }, stores.Calls,
                "the world must remember the fatal run BEFORE the save is consumed");
            Assert.IsFalse(stores.Exists(), "death consumes the in-progress save (FR2)");
            lifecycle.Dispose();
        }

        [Test]
        public void Defeat_ReturnsToTheHub_AfterConsumeAndMark_O1()
        {
            var stores = new RecordingStores { Saved = new RunSaveSnapshot() };
            var relay = new CombatOutcomeRelay();
            var lifecycle = new RunLifecycleService(relay, stores, stores,
                new RecordingLoader(stores.Calls), new RecordingArrival(stores.Calls));
            lifecycle.Initialize();

            relay.Notify(CombatPhase.Defeat);

            CollectionAssert.AreEqual(
                new[] { "meta-flush", "run-delete", "mark-death-return", "load:" + SceneNames.Hub },
                stores.Calls,
                "death must flush, consume, mark the arrival, and only then load the Hub");
            lifecycle.Dispose();
        }

        [Test]
        public void Victory_LoadsNothing()
        {
            var stores = new RecordingStores { Saved = new RunSaveSnapshot() };
            var relay = new CombatOutcomeRelay();
            var lifecycle = new RunLifecycleService(relay, stores, stores,
                new RecordingLoader(stores.Calls), new RecordingArrival(stores.Calls));
            lifecycle.Initialize();

            relay.Notify(CombatPhase.Victory);

            Assert.IsEmpty(stores.Calls, "a victory must not navigate anywhere");
            lifecycle.Dispose();
        }

        [Test]
        public void Victory_TouchesNothing()
        {
            var stores = new RecordingStores { Saved = new RunSaveSnapshot() };
            var relay = new CombatOutcomeRelay();
            var lifecycle = new RunLifecycleService(relay, stores, stores);
            lifecycle.Initialize();

            relay.Notify(CombatPhase.Victory);

            Assert.IsEmpty(stores.Calls);
            Assert.IsTrue(stores.Exists());
            lifecycle.Dispose();
        }

        [Test]
        public void DisposedLifecycle_IgnoresLaterOutcomes()
        {
            var stores = new RecordingStores { Saved = new RunSaveSnapshot() };
            var relay = new CombatOutcomeRelay();
            var lifecycle = new RunLifecycleService(relay, stores, stores);
            lifecycle.Initialize();
            lifecycle.Dispose();

            relay.Notify(CombatPhase.Defeat);
            Assert.IsEmpty(stores.Calls);
        }

        [Test]
        public void Autosave_WritesRunImageAndFlushesMeta()
        {
            var stores = new RecordingStores();
            var autosave = new AutosaveService(new FakeRunState(), stores, stores,
                () => DialogueRunnerState.Ended);

            autosave.Save();

            CollectionAssert.AreEqual(new[] { "run-save", "meta-flush" }, stores.Calls);
            Assert.IsNotNull(stores.Saved);
        }

        [Test]
        public void Autosave_SkipsSavepointWhileDialogueAwaitsExternal_W3_1()
        {
            var stores = new RecordingStores();
            var autosave = new AutosaveService(new FakeRunState(), stores, stores,
                () => DialogueRunnerState.AwaitingExternal);

            autosave.Save();

            Assert.IsEmpty(stores.Calls, "a suspended dialogue is a non-savepoint; nothing is written");
        }

        [Test]
        public void GracefulQuitSave_WritesOnlyWhenNoDialogueIsOpen()
        {
            // Quit outside a conversation: current-platform progress is captured.
            var idle = new RecordingStores();
            new AutosaveService(new FakeRunState(), idle, idle, () => DialogueRunnerState.Ended)
                .SaveGraceful();
            CollectionAssert.AreEqual(new[] { "run-save", "meta-flush" }, idle.Calls);

            // Quit mid-conversation (any open state): the entry-time save stands — the platform
            // re-begins clean on resume (FR7), never half-conversed.
            foreach (var open in new[]
                     {
                         DialogueRunnerState.Running,
                         DialogueRunnerState.AwaitingContinue,
                         DialogueRunnerState.AwaitingExternal
                     })
            {
                var stores = new RecordingStores();
                new AutosaveService(new FakeRunState(), stores, stores, () => open).SaveGraceful();
                Assert.IsEmpty(stores.Calls, $"quit save must skip while the dialogue is {open}");
            }
        }
    }
}
