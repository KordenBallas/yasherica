using System;
using System.IO;
using Core.Persistence;
using LevelGeneration;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 Hub persistence: the one-shot run-setup carrier (Hub → Area), the hub-arrival marker
    /// (Area → Hub), and the RunStartConditions resolution (restore wins, fresh consumes, nothing
    /// yields defaults). Real file IO against a per-test temp directory, real JsonUtility serializer
    /// — the same harness as PersistenceStoreTests.
    /// </summary>
    [TestFixture]
    public class HubPersistenceTests
    {
        private string _directory;
        private UnityJsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "yasherica-hub-tests-" + Guid.NewGuid().ToString("N"));
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

        private string SetupPath => Path.Combine(_directory, RunSetupStore.FileName);

        private RunSetupStore NewSetupStore() => new RunSetupStore(_directory, _serializer);
        private HubArrivalStore NewArrivalStore() => new HubArrivalStore(_directory, _serializer);

        // ---- RunSetupStore ---------------------------------------------------------------------

        [Test]
        public void RunSetup_RoundTripsBothChoices()
        {
            var store = NewSetupStore();
            store.Save(new RunSetupSnapshot
            {
                StartingPartId = "part_head_b",
                StartingBiome = nameof(LevelTheme.Desert)
            });

            Assert.IsTrue(NewSetupStore().TryLoad(out var loaded));
            Assert.AreEqual("part_head_b", loaded.StartingPartId);
            Assert.AreEqual("Desert", loaded.StartingBiome);
            Assert.AreEqual(RunSetupSnapshot.CurrentVersion, loaded.Version);
        }

        [Test]
        public void RunSetup_MissingFile_LoadsFalse()
        {
            Assert.IsFalse(NewSetupStore().TryLoad(out var loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void RunSetup_Delete_ConsumesTheFile()
        {
            var store = NewSetupStore();
            store.Save(new RunSetupSnapshot());
            store.Delete();
            Assert.IsFalse(store.TryLoad(out _));
        }

        [Test]
        public void RunSetup_CorruptFile_IsDiscarded()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SetupPath, "{ not json !!");

            Assert.IsFalse(NewSetupStore().TryLoad(out _));
            Assert.IsFalse(File.Exists(SetupPath), "corrupt run-setup must be deleted, never crash");
        }

        [Test]
        public void RunSetup_ForeignVersion_IsTreatedAsCorrupt()
        {
            var store = NewSetupStore();
            store.Save(new RunSetupSnapshot());
            var json = File.ReadAllText(SetupPath);
            File.WriteAllText(SetupPath, json.Replace(
                "\"Version\": " + RunSetupSnapshot.CurrentVersion,
                "\"Version\": " + (RunSetupSnapshot.CurrentVersion + 999)));

            Assert.IsFalse(NewSetupStore().TryLoad(out _));
            Assert.IsFalse(File.Exists(SetupPath));
        }

        // ---- HubArrivalStore -------------------------------------------------------------------

        [Test]
        public void HubArrival_MarkThenConsume_TrueOnceThenFalse()
        {
            var store = NewArrivalStore();
            store.MarkDeathReturn();

            Assert.IsTrue(store.TryConsumeDeathReturn(), "the marked death return must be consumable once");
            Assert.IsFalse(store.TryConsumeDeathReturn(), "consuming must delete the marker");
        }

        [Test]
        public void HubArrival_NothingMarked_ConsumesFalse()
        {
            Assert.IsFalse(NewArrivalStore().TryConsumeDeathReturn());
        }

        [Test]
        public void HubArrival_CorruptMarker_ConsumesFalseAndClears()
        {
            Directory.CreateDirectory(_directory);
            var path = Path.Combine(_directory, HubArrivalStore.FileName);
            File.WriteAllText(path, "garbage");

            Assert.IsFalse(NewArrivalStore().TryConsumeDeathReturn());
            Assert.IsFalse(File.Exists(path));
        }

        // ---- RunStartConditions ----------------------------------------------------------------

        [Test]
        public void Resolve_Restoring_TakesBiomeFromSnapshot_AndDeletesStaleSetup()
        {
            var setupStore = NewSetupStore();
            setupStore.Save(new RunSetupSnapshot
            {
                StartingPartId = "stale_part",
                StartingBiome = nameof(LevelTheme.Forest)
            });
            var restore = new RunRestoreContext(new RunSaveSnapshot
            {
                StartingBiome = nameof(LevelTheme.Mountain)
            });

            var conditions = RunStartConditions.Resolve(restore, setupStore);

            Assert.AreEqual(string.Empty, conditions.StartingPartId,
                "on a restore the part rides the body snapshot, never the setup file");
            Assert.IsTrue(conditions.TryGetStartingTheme(out var theme));
            Assert.AreEqual(LevelTheme.Mountain, theme);
            Assert.IsFalse(setupStore.TryLoad(out _), "a stale setup file must be deleted on restore");
        }

        [Test]
        public void Resolve_Fresh_ConsumesTheSetupFileOnRead()
        {
            var setupStore = NewSetupStore();
            setupStore.Save(new RunSetupSnapshot
            {
                StartingPartId = "part_torso_b",
                StartingBiome = nameof(LevelTheme.Desert)
            });

            var conditions = RunStartConditions.Resolve(new RunRestoreContext(null), setupStore);

            Assert.AreEqual("part_torso_b", conditions.StartingPartId);
            Assert.IsTrue(conditions.TryGetStartingTheme(out var theme));
            Assert.AreEqual(LevelTheme.Desert, theme);
            Assert.IsFalse(setupStore.TryLoad(out _), "the setup must be consumed on read (one-shot)");
        }

        [Test]
        public void Resolve_FreshWithNothingOnDisk_YieldsEmptyDefaults()
        {
            var conditions = RunStartConditions.Resolve(new RunRestoreContext(null), NewSetupStore());

            Assert.AreEqual(string.Empty, conditions.StartingPartId);
            Assert.IsFalse(conditions.TryGetStartingTheme(out _),
                "no biome name must mean the seeded default, not a parsed theme");
        }

        [Test]
        public void TryGetStartingTheme_GarbageAndNumericNames_AreRejected()
        {
            Assert.IsFalse(new RunStartConditions("", "Swamp").TryGetStartingTheme(out _));
            // Enum.TryParse parses raw numerals; an undefined numeric value must still be rejected.
            Assert.IsFalse(new RunStartConditions("", "7").TryGetStartingTheme(out _));
        }
    }
}
