using Narrative.Director.Core;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
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
