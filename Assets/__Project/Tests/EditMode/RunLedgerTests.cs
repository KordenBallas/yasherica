using Core.Persistence;
using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunLedgerTests
    {
        [Test]
        public void Record_IsIdempotentAndIgnoresInvalidInput()
        {
            var ledger = new RunLedger();
            ledger.RecordInstalledPart(1, "part_a");
            ledger.RecordInstalledPart(1, "part_a");
            ledger.RecordInstalledPart(0, "part_b");
            ledger.RecordInstalledPart(1, null);
            ledger.RecordInstalledPart(1, string.Empty);
            ledger.RecordSocketedArtifact(1, "art_x");
            ledger.RecordSocketedArtifact(1, "art_x");

            var snapshot = ledger.Capture(8);

            Assert.AreEqual(1, snapshot.Runs.Count);
            Assert.AreEqual(new[] { "part_a" }, snapshot.Runs[0].InstalledPartIds);
            Assert.AreEqual(new[] { "art_x" }, snapshot.Runs[0].SocketedArtifactIds);
        }

        [Test]
        public void Capture_IsSortedByRunIndexAndOrdinalIds()
        {
            var ledger = new RunLedger();
            ledger.RecordInstalledPart(3, "part_z");
            ledger.RecordInstalledPart(3, "part_a");
            ledger.RecordInstalledPart(1, "part_m");

            var snapshot = ledger.Capture(8);

            Assert.AreEqual(2, snapshot.Runs.Count);
            Assert.AreEqual(1, snapshot.Runs[0].RunIndex);
            Assert.AreEqual(3, snapshot.Runs[1].RunIndex);
            Assert.AreEqual(new[] { "part_a", "part_z" }, snapshot.Runs[1].InstalledPartIds);
        }

        [Test]
        public void Capture_PrunesToTheLastRetainedRuns()
        {
            var ledger = new RunLedger();
            for (int run = 1; run <= 5; run++)
            {
                ledger.RecordInstalledPart(run, $"part_{run}");
            }

            var snapshot = ledger.Capture(2);

            Assert.AreEqual(2, snapshot.Runs.Count);
            Assert.AreEqual(4, snapshot.Runs[0].RunIndex);
            Assert.AreEqual(5, snapshot.Runs[1].RunIndex);
        }

        [Test]
        public void FromSnapshot_RoundTripsThroughCapture()
        {
            var ledger = new RunLedger();
            ledger.RecordInstalledPart(2, "part_a");
            ledger.RecordSocketedArtifact(2, "art_b");
            ledger.RecordInstalledPart(4, "part_c");

            var restored = RunLedger.FromSnapshot(ledger.Capture(8));
            var snapshot = restored.Capture(8);

            Assert.AreEqual(2, snapshot.Runs.Count);
            Assert.AreEqual(new[] { "part_a" }, snapshot.Runs[0].InstalledPartIds);
            Assert.AreEqual(new[] { "art_b" }, snapshot.Runs[0].SocketedArtifactIds);
            Assert.AreEqual(new[] { "part_c" }, snapshot.Runs[1].InstalledPartIds);
        }

        [Test]
        public void FromSnapshot_ToleratesNullAndMalformedEntries()
        {
            var malformed = new MetaRunLedgerSnapshot();
            malformed.Runs.Add(null);
            malformed.Runs.Add(new RunLedgerEntryDto { RunIndex = 1, InstalledPartIds = null, SocketedArtifactIds = null });

            // No throw; an id-less run holds no direction data, so no entry survives.
            var snapshot = RunLedger.FromSnapshot(malformed).Capture(8);

            Assert.IsEmpty(snapshot.Runs);
        }

        [Test]
        public void NewRunLedger_CapturesEmpty()
        {
            Assert.IsEmpty(new RunLedger().Capture(8).Runs);
            Assert.IsEmpty(RunLedger.FromSnapshot(null).Capture(8).Runs);
        }

        [Test]
        public void EffectiveRunCount_FreshAddsOne_ContinueKeepsStored()
        {
            var facts = new FactStoreSnapshot();
            facts.Entries.Add(new FactEntryDto
            {
                Namespace = FactNamespace.World,
                Subject = string.Empty,
                Key = "run_count",
                Type = FactValueType.Int,
                IntValue = 4
            });

            Assert.AreEqual(5, EffectiveRunCount.From(facts, servesFreshRun: true));
            Assert.AreEqual(4, EffectiveRunCount.From(facts, servesFreshRun: false));
        }

        [Test]
        public void EffectiveRunCount_EmptyStore_IsFirstEverRun()
        {
            Assert.AreEqual(1, EffectiveRunCount.From(null, servesFreshRun: true));
            Assert.AreEqual(1, EffectiveRunCount.From(new FactStoreSnapshot(), servesFreshRun: true));
            Assert.AreEqual(0, EffectiveRunCount.From(null, servesFreshRun: false));
        }
    }
}
