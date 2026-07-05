using Narrative.Director.Core;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class NarrativeSnapshotTests
    {
        [Test]
        public void FactStore_Capture_Restore_RoundTripsValues()
        {
            var source = new FactStore();
            source.Set(new FactKey(FactNamespace.World, "", "pass_cleared"), FactValue.FromBool(true));
            source.Set(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(7));
            source.Set(new FactKey(FactNamespace.Actor, "npc_07", "name"), FactValue.FromString("Razor"));

            var snapshot = FactStoreSnapshotMapper.Capture(source);

            var restored = new FactStore();
            FactStoreSnapshotMapper.Restore(snapshot, restored);

            Assert.IsTrue(restored.GetOrDefault(new FactKey(FactNamespace.World, "", "pass_cleared"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(7, restored.GetOrDefault(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(0)).AsInt());
            Assert.AreEqual("Razor", restored.GetOrDefault(new FactKey(FactNamespace.Actor, "npc_07", "name"), FactValue.FromString("")).AsString());
        }

        [Test]
        public void Capture_EmitsEntriesInStableOrder()
        {
            var store = new FactStore();
            store.Set(new FactKey(FactNamespace.Faction, "z", "k"), FactValue.FromInt(1));
            store.Set(new FactKey(FactNamespace.World, "", "a"), FactValue.FromInt(1));

            var snapshot = FactStoreSnapshotMapper.Capture(store);
            Assert.AreEqual("a", snapshot.Entries[0].Key);
            Assert.AreEqual(FactNamespace.World, snapshot.Entries[0].Namespace);
            Assert.AreEqual(FactNamespace.Faction, snapshot.Entries[1].Namespace);
        }

        [Test]
        public void Capture_SplitsRunAndMetaByHorizon_D20()
        {
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "pass_cleared", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "spine_cursor", FactScope.Global, FactValueType.Int, FactValue.FromInt(0), FactHorizon.Meta)
            });
            var store = new FactStore();
            store.Set(new FactKey(FactNamespace.World, "", "pass_cleared"), FactValue.FromBool(true));
            store.Set(new FactKey(FactNamespace.World, "", "spine_cursor"), FactValue.FromInt(2));
            // A key outside the vocabulary partitions as run-scoped (conservative side).
            store.Set(new FactKey(FactNamespace.World, "", "undeclared"), FactValue.FromBool(true));

            var service = new NarrativeSaveService(store, new DeterministicRandom(1), seed: 1, registry: registry);
            Assert.IsTrue(service.TryCapture(DialogueRunnerState.Running, out var snapshot));

            Assert.AreEqual(2, snapshot.Facts.Entries.Count);
            Assert.AreEqual(1, snapshot.MetaFacts.Entries.Count);
            Assert.AreEqual("spine_cursor", snapshot.MetaFacts.Entries[0].Key);
            CollectionAssert.AreEquivalent(new[] { "pass_cleared", "undeclared" },
                new[] { snapshot.Facts.Entries[0].Key, snapshot.Facts.Entries[1].Key });
        }

        [Test]
        public void Restore_RoundTripsBothHorizons_D20()
        {
            var registry = new FactKeyRegistry(new[]
            {
                new FactKeyInfo(FactNamespace.World, "run_fact", FactScope.Global, FactValueType.Bool, FactValue.FromBool(false)),
                new FactKeyInfo(FactNamespace.World, "meta_fact", FactScope.Global, FactValueType.Int, FactValue.FromInt(0), FactHorizon.Meta)
            });
            var source = new FactStore();
            source.Set(new FactKey(FactNamespace.World, "", "run_fact"), FactValue.FromBool(true));
            source.Set(new FactKey(FactNamespace.World, "", "meta_fact"), FactValue.FromInt(9));

            var service = new NarrativeSaveService(source, new DeterministicRandom(1), seed: 1, registry: registry);
            Assert.IsTrue(service.TryCapture(DialogueRunnerState.Running, out var snapshot));

            var restoredStore = new FactStore();
            var restoreService = new NarrativeSaveService(restoredStore, new DeterministicRandom(0), seed: 0, registry: registry);
            restoreService.Restore(snapshot);

            Assert.IsTrue(restoredStore.GetOrDefault(new FactKey(FactNamespace.World, "", "run_fact"), FactValue.FromBool(false)).AsBool());
            Assert.AreEqual(9, restoredStore.GetOrDefault(new FactKey(FactNamespace.World, "", "meta_fact"), FactValue.FromInt(0)).AsInt());
        }

        [Test]
        public void SaveService_RefusesCaptureWhileSuspended_W3_1()
        {
            var store = new FactStore();
            var service = new NarrativeSaveService(store, new DeterministicRandom(5), seed: 5);

            Assert.IsFalse(service.CanCapture(DialogueRunnerState.AwaitingExternal));
            Assert.IsFalse(service.TryCapture(DialogueRunnerState.AwaitingExternal, out var refused));
            Assert.IsNull(refused);

            Assert.IsTrue(service.TryCapture(DialogueRunnerState.Running, out var ok));
            Assert.IsNotNull(ok);
        }

        [Test]
        public void Ledgers_CaptureAndRestore_RoundTripThreadLifecycleAndStoryRecord()
        {
            var threads = new ThreadLedger();
            threads.Open("barn_raid", ThreadKind.Ephemeral, 0);
            threads.NoteBeatResolved("barn_raid");
            threads.Open("frog_marsh", ThreadKind.Arc, 1);
            threads.Open("errand", ThreadKind.Ephemeral, 1);
            threads.Fail("errand", ThreadRetirementReason.Expired);
            var stories = new StoryRunLedger();
            stories.NotePlaced("story_barn_victim", "barn_raid", 0);
            stories.NoteResolved("story_barn_victim");
            stories.NotePlaced("story_frog_elder", "frog_marsh", 1);

            var service = new NarrativeSaveService(new FactStore(), new DeterministicRandom(1), seed: 1,
                threads: threads, stories: stories);
            Assert.IsTrue(service.TryCapture(DialogueRunnerState.Running, out var snapshot));

            var restoredThreads = new ThreadLedger();
            var restoredStories = new StoryRunLedger();
            var restoreService = new NarrativeSaveService(new FactStore(), new DeterministicRandom(0), seed: 0,
                threads: restoredThreads, stories: restoredStories);
            restoreService.Restore(snapshot);

            Assert.AreEqual(3, restoredThreads.Threads.Count);
            Assert.AreEqual("barn_raid", restoredThreads.Threads[0].ThreadId); // registration order kept
            Assert.IsTrue(restoredThreads.TryGet("barn_raid", out var barn));
            Assert.AreEqual(1, barn.Stage);
            Assert.IsTrue(barn.AdvancedSinceLastTick);
            Assert.IsTrue(restoredThreads.TryGet("frog_marsh", out var frog));
            Assert.AreEqual(ThreadKind.Arc, frog.Kind);
            Assert.AreEqual(ThreadState.Live, frog.State);
            Assert.IsTrue(restoredThreads.TryGet("errand", out var errand));
            Assert.AreEqual(ThreadState.Failed, errand.State);
            Assert.AreEqual(ThreadRetirementReason.Expired, errand.Reason);

            Assert.IsTrue(restoredStories.TryGet("story_barn_victim", out var victim));
            Assert.AreEqual(StoryRunStatus.Resolved, victim.Status);
            Assert.IsTrue(restoredStories.TryGet("story_frog_elder", out var elder));
            Assert.AreEqual(StoryRunStatus.Placed, elder.Status);
            Assert.AreEqual(1, elder.WindowPlaced);
        }

        [Test]
        public void SaveService_CapturesAndRestoresRngState_B2()
        {
            var store = new FactStore();
            var rng = new DeterministicRandom(123);
            rng.NextInt(100);
            rng.NextInt(100);

            var service = new NarrativeSaveService(store, rng, seed: 123);
            Assert.IsTrue(service.TryCapture(DialogueRunnerState.Running, out var snapshot));
            Assert.AreEqual(rng.State, snapshot.RngState);

            // Advance, then restore: subsequent draws match the captured continuation.
            var expected = new[] { rng.NextInt(100), rng.NextInt(100) };

            var rng2 = new DeterministicRandom(0);
            var service2 = new NarrativeSaveService(store, rng2, seed: 0);
            service2.Restore(snapshot);
            Assert.AreEqual(expected[0], rng2.NextInt(100));
            Assert.AreEqual(expected[1], rng2.NextInt(100));
        }
    }
}
