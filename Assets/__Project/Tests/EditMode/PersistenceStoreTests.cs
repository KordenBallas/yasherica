using System;
using System.IO;
using Core.Persistence;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// P2-2 persistence foundation: the versioned/atomic file gateway (FR13/FR14 fail-safe), the
    /// run-save and cross-run-memory stores, and the meta flush/bootstrap round trip (FR9/FR11).
    /// Uses real file IO against a per-test temp directory and the real JsonUtility serializer.
    /// </summary>
    [TestFixture]
    public class PersistenceStoreTests
    {
        private string _directory;
        private UnityJsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "yasherica-save-tests-" + Guid.NewGuid().ToString("N"));
            _serializer = new UnityJsonSaveSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private string RunPath => Path.Combine(_directory, RunSaveStore.FileName);
        private string MetaPath => Path.Combine(_directory, MetaMemoryStore.FileName);

        // ---- JsonSaveFile / RunSaveStore -------------------------------------------------------

        [Test]
        public void RunSave_RoundTripsThroughDisk()
        {
            var store = new RunSaveStore(_directory, _serializer);
            var snapshot = new RunSaveSnapshot();
            snapshot.Narrative.Seed = 42;
            snapshot.Narrative.Facts.Entries.Add(new FactEntryDto
            {
                Namespace = FactNamespace.World,
                Subject = string.Empty,
                Key = "pass_cleared",
                Type = FactValueType.Bool,
                BoolValue = true
            });

            Assert.IsFalse(store.Exists());
            store.Save(snapshot);
            Assert.IsTrue(store.Exists());

            var reloaded = new RunSaveStore(_directory, _serializer);
            Assert.IsTrue(reloaded.TryLoad(out var loaded));
            Assert.AreEqual(RunSaveSnapshot.CurrentVersion, loaded.Version);
            Assert.AreEqual(42, loaded.Narrative.Seed);
            Assert.AreEqual("pass_cleared", loaded.Narrative.Facts.Entries[0].Key);
            Assert.IsTrue(loaded.Narrative.Facts.Entries[0].BoolValue);
            Assert.Greater(loaded.SavedAtUtcTicks, 0L);
        }

        [Test]
        public void RunSave_UlongRngState_RoundTripsHighBits()
        {
            // D1 risk check: JsonUtility must round-trip a ulong with the high bit set exactly.
            var store = new RunSaveStore(_directory, _serializer);
            var snapshot = new RunSaveSnapshot();
            snapshot.Narrative.RngState = 0xDEADBEEF_DEADBEEF;

            store.Save(snapshot);
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(0xDEADBEEF_DEADBEEFul, loaded.Narrative.RngState);
        }

        [Test]
        public void RunSave_MissingFile_LoadsFalseWithoutCorruptHandling()
        {
            var store = new RunSaveStore(_directory, _serializer);
            Assert.IsFalse(store.TryLoad(out var loaded));
            Assert.IsNull(loaded);
            Assert.IsFalse(Directory.Exists(_directory) && File.Exists(RunPath + ".corrupt"));
        }

        [Test]
        public void RunSave_CorruptFile_IsDiscardedAndReportsAbsent()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(RunPath, "{ not valid json !!");

            var store = new RunSaveStore(_directory, _serializer);
            Assert.IsFalse(store.TryLoad(out _));
            Assert.IsFalse(File.Exists(RunPath), "corrupt run save must be deleted (FR13)");
        }

        [Test]
        public void RunSave_VersionMismatch_IsTreatedAsCorrupt()
        {
            var store = new RunSaveStore(_directory, _serializer);
            var snapshot = new RunSaveSnapshot();
            store.Save(snapshot);

            // Simulate a file written by another build: patch the version on disk (FR14).
            var json = File.ReadAllText(RunPath);
            File.WriteAllText(RunPath, json.Replace(
                "\"Version\": " + RunSaveSnapshot.CurrentVersion,
                "\"Version\": " + (RunSaveSnapshot.CurrentVersion + 999)));

            Assert.IsFalse(store.TryLoad(out _));
            Assert.IsFalse(File.Exists(RunPath), "stale-version run save must be discarded, never crash (FR14)");
        }

        [Test]
        public void RunSave_Delete_ConsumesTheSave()
        {
            var store = new RunSaveStore(_directory, _serializer);
            store.Save(new RunSaveSnapshot());
            Assert.IsTrue(store.Exists());

            store.Delete();
            Assert.IsFalse(store.Exists());
            Assert.IsFalse(store.TryLoad(out _));
        }

        [Test]
        public void Save_OverwritesAtomically_LeavesNoTempFile()
        {
            var store = new RunSaveStore(_directory, _serializer);
            store.Save(new RunSaveSnapshot());
            var first = File.ReadAllText(RunPath);

            var second = new RunSaveSnapshot();
            second.Narrative.Seed = 7;
            store.Save(second);

            Assert.IsFalse(File.Exists(RunPath + ".tmp"), "temp file must be swapped away");
            Assert.AreNotEqual(first, File.ReadAllText(RunPath));
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(7, loaded.Narrative.Seed);
        }

        // ---- MetaMemoryStore ---------------------------------------------------------------

        [Test]
        public void MetaMemory_MissingFile_LoadsEmptyWithoutCreatingFiles()
        {
            var store = new MetaMemoryStore(_directory, _serializer);
            var memory = store.LoadOrEmpty();
            Assert.IsNotNull(memory);
            Assert.AreEqual(0, memory.Facts.Entries.Count);
        }

        [Test]
        public void MetaMemory_CorruptFile_IsQuarantinedAndDegradesToEmpty()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(MetaPath, "garbage");

            var store = new MetaMemoryStore(_directory, _serializer);
            var memory = store.LoadOrEmpty();

            Assert.AreEqual(0, memory.Facts.Entries.Count, "corrupt world memory degrades to blank slate (FR13)");
            Assert.IsFalse(File.Exists(MetaPath));
            Assert.IsTrue(File.Exists(MetaPath + ".corrupt"), "world memory is quarantined, never silently destroyed");
        }

        [Test]
        public void CorruptMetaMemory_LeavesRunSaveUntouched_AndViceVersa()
        {
            // FR13: the two files are independently recoverable.
            var runStore = new RunSaveStore(_directory, _serializer);
            runStore.Save(new RunSaveSnapshot());
            File.WriteAllText(MetaPath, "garbage");

            var metaStore = new MetaMemoryStore(_directory, _serializer);
            metaStore.LoadOrEmpty();
            Assert.IsTrue(runStore.Exists(), "corrupt meta must not touch the run save");

            metaStore.Save(new MetaMemorySnapshot());
            File.WriteAllText(RunPath, "garbage");
            Assert.IsFalse(runStore.TryLoad(out _));
            var memory = metaStore.LoadOrEmpty();
            Assert.IsNotNull(memory, "corrupt run save must not touch the world memory");
            Assert.IsTrue(File.Exists(MetaPath));
        }

        // ---- Cross-run memory end-to-end (FR9/FR11) ---------------------------------------

        private static FactKeyRegistry DemoRegistry() => new FactKeyRegistry(new[]
        {
            new FactKeyInfo(FactNamespace.World, "run_fact", FactScope.Global, FactValueType.Bool,
                FactValue.FromBool(false)),
            new FactKeyInfo(FactNamespace.World, "barn_bounty_honored", FactScope.Global, FactValueType.Bool,
                FactValue.FromBool(false), FactHorizon.Meta)
        });

        [Test]
        public void MetaFact_SetInRunOne_IsReadableInRunTwo_AndRunFactIsNot()
        {
            var registry = DemoRegistry();
            var metaStore = new MetaMemoryStore(_directory, _serializer);

            // Run 1: both facts get set in-fiction; the savepoint flushes the Meta partition.
            var runOneFacts = new FactStore();
            runOneFacts.Set(new FactKey(FactNamespace.World, "", "run_fact"), FactValue.FromBool(true));
            runOneFacts.Set(new FactKey(FactNamespace.World, "", "barn_bounty_honored"), FactValue.FromBool(true));
            new MetaMemoryFlushService(metaStore, runOneFacts, registry).Flush();

            // Run 2 (fresh store, fresh scene): the bootstrap loads the world memory.
            var runTwoFacts = new FactStore();
            var freshMetaStore = new MetaMemoryStore(_directory, _serializer);
            new MetaMemoryBootstrap(freshMetaStore, runTwoFacts).Initialize();

            Assert.IsTrue(runTwoFacts
                .GetOrDefault(new FactKey(FactNamespace.World, "", "barn_bounty_honored"), FactValue.FromBool(false))
                .AsBool(), "the world must remember the meta fact across runs (FR11)");
            Assert.IsFalse(runTwoFacts.Has(new FactKey(FactNamespace.World, "", "run_fact")),
                "run-scoped facts must NOT leak across runs (FR9)");
        }

        [Test]
        public void MetaFlush_DoesNotMutateTheLiveFactStore()
        {
            var registry = DemoRegistry();
            var facts = new FactStore();
            facts.Set(new FactKey(FactNamespace.World, "", "run_fact"), FactValue.FromBool(true));
            facts.Set(new FactKey(FactNamespace.World, "", "barn_bounty_honored"), FactValue.FromBool(true));
            var before = facts.Snapshot().Count;

            new MetaMemoryFlushService(new MetaMemoryStore(_directory, _serializer), facts, registry).Flush();

            Assert.AreEqual(before, facts.Snapshot().Count);
            Assert.IsTrue(facts.GetOrDefault(new FactKey(FactNamespace.World, "", "run_fact"),
                FactValue.FromBool(false)).AsBool());
        }
    }
}
