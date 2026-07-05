using Narrative.Threads.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ThreadLedgerTests
    {
        private ThreadLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _ledger = new ThreadLedger();
        }

        [Test]
        public void Open_TracksThreadLive_InFirstOpenOrder()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, windowIndex: 0);
            _ledger.Open("frog_marsh", ThreadKind.Arc, windowIndex: 1);

            Assert.AreEqual(2, _ledger.Threads.Count);
            Assert.AreEqual("barn_raid", _ledger.Threads[0].ThreadId);
            Assert.AreEqual("frog_marsh", _ledger.Threads[1].ThreadId);
            Assert.AreEqual(ThreadState.Live, _ledger.Threads[0].State);
            Assert.AreEqual(2, _ledger.LiveCount);
        }

        [Test]
        public void Open_IsIdempotent_KeepsFirstRecord()
        {
            var first = _ledger.Open("barn_raid", ThreadKind.Ephemeral, windowIndex: 0);
            var again = _ledger.Open("barn_raid", ThreadKind.Arc, windowIndex: 5);

            Assert.AreSame(first, again);
            Assert.AreEqual(ThreadKind.Ephemeral, again.Kind);
            Assert.AreEqual(0, again.WindowOpened);
            Assert.AreEqual(1, _ledger.Threads.Count);
        }

        [Test]
        public void Open_EmptyThreadId_IsIgnored()
        {
            Assert.IsNull(_ledger.Open("", ThreadKind.Ephemeral, 0));
            Assert.IsNull(_ledger.Open(null, ThreadKind.Ephemeral, 0));
            Assert.AreEqual(0, _ledger.Threads.Count);
        }

        [Test]
        public void NoteBeatResolved_AdvancesStage_AndFlagsAdvance()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);

            _ledger.NoteBeatResolved("barn_raid");
            _ledger.NoteBeatResolved("barn_raid");

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(2, record.Stage);
            Assert.IsTrue(record.AdvancedSinceLastTick);
        }

        [Test]
        public void NoteBeatResolved_UnknownOrRetiredThread_IsIgnored()
        {
            _ledger.NoteBeatResolved("never_opened");

            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _ledger.Fail("barn_raid", ThreadRetirementReason.Conflict);
            _ledger.NoteBeatResolved("barn_raid");

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(0, record.Stage);
        }

        [Test]
        public void Fail_RetiresThread_WithReason_AndFreesLiveCount()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _ledger.Fail("barn_raid", ThreadRetirementReason.Expired);

            Assert.IsTrue(_ledger.IsRetired("barn_raid"));
            Assert.AreEqual(0, _ledger.LiveCount);
            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(ThreadState.Failed, record.State);
            Assert.AreEqual(ThreadRetirementReason.Expired, record.Reason);
        }

        [Test]
        public void Resolve_RetiresThread_WithoutFailReason()
        {
            _ledger.Open("frog_marsh", ThreadKind.Arc, 0);
            _ledger.Resolve("frog_marsh");

            Assert.IsTrue(_ledger.IsRetired("frog_marsh"));
            Assert.IsTrue(_ledger.TryGet("frog_marsh", out var record));
            Assert.AreEqual(ThreadState.Resolved, record.State);
            Assert.AreEqual(ThreadRetirementReason.None, record.Reason);
        }

        [Test]
        public void TerminalStateWins_LaterTransitionsIgnored()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            _ledger.Resolve("barn_raid");
            _ledger.Fail("barn_raid", ThreadRetirementReason.Conflict);

            Assert.IsTrue(_ledger.TryGet("barn_raid", out var record));
            Assert.AreEqual(ThreadState.Resolved, record.State);
        }

        [Test]
        public void IsRetired_FalseForLiveAndUnknown()
        {
            _ledger.Open("barn_raid", ThreadKind.Ephemeral, 0);
            Assert.IsFalse(_ledger.IsRetired("barn_raid"));
            Assert.IsFalse(_ledger.IsRetired("never_opened"));
        }
    }

    [TestFixture]
    public class ThreadCatalogTests
    {
        [Test]
        public void TryGet_FindsDeclared_MissesUnknown()
        {
            var catalog = new ThreadCatalog(new[]
            {
                new ThreadDefinitionData("barn_raid", ThreadKind.Ephemeral, null, null, 4)
            }, defaultLifespanWindows: 3);

            Assert.IsTrue(catalog.TryGet("barn_raid", out var declared));
            Assert.AreEqual(4, declared.LifespanWindows);
            Assert.IsFalse(catalog.TryGet("unknown", out _));
        }

        [Test]
        public void GetOrImplicitDefault_UndeclaredLabel_IsEphemeralWithDefaultLifespan()
        {
            var catalog = new ThreadCatalog(null, defaultLifespanWindows: 3);

            var implicitDefault = catalog.GetOrImplicitDefault("bare_label");

            Assert.AreEqual("bare_label", implicitDefault.ThreadId);
            Assert.AreEqual(ThreadKind.Ephemeral, implicitDefault.Kind);
            Assert.AreEqual(3, implicitDefault.LifespanWindows);
            Assert.AreEqual(0, implicitDefault.Premise.Count);
            Assert.AreEqual(0, implicitDefault.ResolutionConditions.Count);
        }
    }
}
