using Core.Persistence;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Guards the additive-field compatibility rule on <see cref="MetaMemorySnapshot"/>:
    /// JsonSaveFile quarantines on a Version mismatch, so the Track-R ledger must ride version 1
    /// as a field an old meta.json simply lacks — deserializing to an empty ledger, never a
    /// quarantine.
    /// </summary>
    [TestFixture]
    public class MetaMemorySnapshotCompatTests
    {
        [Test]
        public void CurrentVersion_StaysOne_SoOldFilesAreNotQuarantined()
        {
            Assert.AreEqual(1, MetaMemorySnapshot.CurrentVersion);
        }

        [Test]
        public void VersionOneJsonWithoutLedger_LoadsWithEmptyLedger()
        {
            // A pre-Track-R meta.json: facts only, no Ledger field.
            const string legacyJson = "{\"Version\":1,\"Facts\":{\"Entries\":[]}}";

            var serializer = new UnityJsonSaveSerializer();
            var snapshot = serializer.FromJson<MetaMemorySnapshot>(legacyJson);

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, snapshot.Version);
            Assert.IsNotNull(snapshot.Ledger);
            Assert.IsEmpty(snapshot.Ledger.Runs);
        }

        [Test]
        public void LedgerRoundTrips_ThroughTheSaveSerializer()
        {
            var serializer = new UnityJsonSaveSerializer();
            var snapshot = new MetaMemorySnapshot { Version = MetaMemorySnapshot.CurrentVersion };
            snapshot.Ledger.Runs.Add(new RunLedgerEntryDto
            {
                RunIndex = 3,
                InstalledPartIds = { "part_a" },
                SocketedArtifactIds = { "art_b" }
            });

            var restored = serializer.FromJson<MetaMemorySnapshot>(serializer.ToJson(snapshot));

            Assert.AreEqual(1, restored.Ledger.Runs.Count);
            Assert.AreEqual(3, restored.Ledger.Runs[0].RunIndex);
            Assert.AreEqual(new[] { "part_a" }, restored.Ledger.Runs[0].InstalledPartIds);
            Assert.AreEqual(new[] { "art_b" }, restored.Ledger.Runs[0].SocketedArtifactIds);
        }
    }
}
