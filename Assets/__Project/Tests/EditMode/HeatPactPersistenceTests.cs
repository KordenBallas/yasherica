using System;
using System.Collections.Generic;
using System.IO;
using Core.Persistence;
using Heat.Core;
using Heat.Integration;
using LevelGeneration;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The Heat pact's persistence ride (heat-ascension FR2/FR4/FR12): additive fields on the two
    /// snapshots at their UNCHANGED versions (old files load to Heat 0, never quarantine), the
    /// Hub→Area/restore carrier, and the DTO↔Core degradation on load. Same temp-dir harness as
    /// HubPersistenceTests.
    /// </summary>
    [TestFixture]
    public class HeatPactPersistenceTests
    {
        private string _directory;
        private UnityJsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "yasherica-heat-tests-" + Guid.NewGuid().ToString("N"));
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

        private RunSetupStore NewSetupStore() => new RunSetupStore(_directory, _serializer);

        private static HeatPactEntryDto Dto(string id, int rank) =>
            new HeatPactEntryDto { ModifierId = id, Rank = rank };

        [Test]
        public void SnapshotVersions_StayUnchanged_SoOldSavesSurviveTheUpgrade()
        {
            Assert.AreEqual(1, RunSetupSnapshot.CurrentVersion);
            Assert.AreEqual(2, RunSaveSnapshot.CurrentVersion);
        }

        [Test]
        public void PreHeatRunSetupJson_LoadsWithAnEmptyPact()
        {
            const string legacyJson =
                "{\"Version\":1,\"StartingPartId\":\"part_x\",\"StartingBiome\":\"Forest\"}";

            var snapshot = _serializer.FromJson<RunSetupSnapshot>(legacyJson);

            Assert.IsNotNull(snapshot.Heat);
            Assert.IsEmpty(snapshot.Heat);
        }

        [Test]
        public void PreHeatRunSaveJson_LoadsWithAnEmptyPact()
        {
            const string legacyJson = "{\"Version\":2,\"RunSeed\":7,\"StartingBiome\":\"Desert\"}";

            var snapshot = _serializer.FromJson<RunSaveSnapshot>(legacyJson);

            Assert.IsNotNull(snapshot.Heat);
            Assert.IsEmpty(snapshot.Heat);
        }

        [Test]
        public void RunSetup_RoundTripsThePact()
        {
            var store = NewSetupStore();
            store.Save(new RunSetupSnapshot
            {
                StartingBiome = nameof(LevelTheme.Forest),
                Heat = new List<HeatPactEntryDto> { Dto("raised-floor", 2), Dto("enemies-first", 1) }
            });

            Assert.IsTrue(NewSetupStore().TryLoad(out var loaded));
            Assert.AreEqual(2, loaded.Heat.Count);
            Assert.AreEqual("raised-floor", loaded.Heat[0].ModifierId);
            Assert.AreEqual(2, loaded.Heat[0].Rank);
        }

        [Test]
        public void Resolve_Fresh_CarriesThePactFromTheSetup()
        {
            var setupStore = NewSetupStore();
            setupStore.Save(new RunSetupSnapshot
            {
                Heat = new List<HeatPactEntryDto> { Dto("stingy-cauldron", 1) }
            });

            var conditions = RunStartConditions.Resolve(new RunRestoreContext(null), setupStore);

            Assert.AreEqual(1, conditions.HeatPact.Count);
            Assert.AreEqual("stingy-cauldron", conditions.HeatPact[0].ModifierId);
        }

        [Test]
        public void Resolve_Restoring_ThePactRidesTheSave_NotTheStaleSetup()
        {
            var setupStore = NewSetupStore();
            setupStore.Save(new RunSetupSnapshot
            {
                Heat = new List<HeatPactEntryDto> { Dto("stale-pact", 1) }
            });
            var restore = new RunRestoreContext(new RunSaveSnapshot
            {
                Heat = new List<HeatPactEntryDto> { Dto("raised-floor", 1) }
            });

            var conditions = RunStartConditions.Resolve(restore, setupStore);

            Assert.AreEqual(1, conditions.HeatPact.Count);
            Assert.AreEqual("raised-floor", conditions.HeatPact[0].ModifierId);
            Assert.IsFalse(setupStore.TryLoad(out _), "the stale setup file is consumed");
        }

        [Test]
        public void DtoMapper_RecomputesTheTotal_AndDegradesStaleEntries()
        {
            var pact = HeatPactDtoMapper.ToPact(HeatPactTests.DemoSettings(), new[]
            {
                Dto("raised-floor", 9), // clamps to max rank 2
                Dto("gone-from-the-menu", 1) // drops
            });

            Assert.AreEqual(2, pact.RankOf("raised-floor"));
            Assert.AreEqual(4, pact.TotalHeat, "the total is recomputed from the menu, never trusted");

            var roundTripped = HeatPactDtoMapper.ToDtos(pact);
            Assert.AreEqual(1, roundTripped.Count);
            Assert.AreEqual("raised-floor", roundTripped[0].ModifierId);
        }
    }
}
