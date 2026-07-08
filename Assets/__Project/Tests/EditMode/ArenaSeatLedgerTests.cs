using System.Collections.Generic;
using Combat.Arena.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The X1 seat book: disconnect grace countdown, expiry into departure, and the derived
    /// rejoin-token gate (Gracing → Resyncing → Connected).
    /// </summary>
    [TestFixture]
    public class ArenaSeatLedgerTests
    {
        private const int MatchSeed = 12345;
        private const int GraceRounds = 3;

        private static List<ArenaRosterSlot> Roster() => new List<ArenaRosterSlot>
        {
            new ArenaRosterSlot(0, 1, 1),
            new ArenaRosterSlot(7, 2, 2),
            new ArenaRosterSlot(9, 3, 3)
        };

        private ArenaSeatLedger CreateSeededLedger()
        {
            var ledger = new ArenaSeatLedger();
            ledger.Seed(MatchSeed, Roster(), GraceRounds);
            return ledger;
        }

        [Test]
        public void FreshLedger_AllSeatsConnected_NothingAbsent()
        {
            var ledger = CreateSeededLedger();

            Assert.AreEqual(ArenaSeatConnection.Connected, ledger.ConnectionOf(1));
            Assert.AreEqual(ArenaSeatConnection.Connected, ledger.ConnectionOf(2));
            CollectionAssert.IsEmpty(ledger.AbsentPlayerIds());
            CollectionAssert.IsEmpty(ledger.TickRound());
        }

        [Test]
        public void Disconnected_RidesExactlyGraceRounds_ThenExpires()
        {
            var ledger = CreateSeededLedger();
            ledger.MarkDisconnected(2);

            Assert.AreEqual(ArenaSeatConnection.Gracing, ledger.ConnectionOf(2));
            CollectionAssert.AreEqual(new[] { 2 }, ledger.AbsentPlayerIds());

            // Exactly GraceRounds round-opens pass the seat; the next one expires it.
            for (int round = 0; round < GraceRounds; round++)
            {
                CollectionAssert.IsEmpty(ledger.TickRound(), $"grace round {round + 1} must not expire");
                Assert.AreEqual(ArenaSeatConnection.Gracing, ledger.ConnectionOf(2));
            }

            CollectionAssert.AreEqual(new[] { 2 }, ledger.TickRound());
            Assert.AreEqual(ArenaSeatConnection.Departed, ledger.ConnectionOf(2));
            CollectionAssert.IsEmpty(ledger.AbsentPlayerIds());
        }

        [Test]
        public void ZeroGrace_ExpiresOnTheNextRoundOpen()
        {
            var ledger = new ArenaSeatLedger();
            ledger.Seed(MatchSeed, Roster(), graceRounds: 0);
            ledger.MarkDisconnected(3);

            CollectionAssert.AreEqual(new[] { 3 }, ledger.TickRound());
            Assert.AreEqual(ArenaSeatConnection.Departed, ledger.ConnectionOf(3));
        }

        [Test]
        public void Rejoin_WithCorrectToken_WithinGrace_ReturnsSeatToConnected()
        {
            var ledger = CreateSeededLedger();
            ledger.MarkDisconnected(2);
            ledger.TickRound();

            var token = ArenaRejoinToken.For(MatchSeed, 2);
            Assert.IsTrue(ledger.TryBeginRejoin(token, newClientId: 42, playerId: 2));
            Assert.AreEqual(ArenaSeatConnection.Resyncing, ledger.ConnectionOf(2));
            CollectionAssert.AreEqual(new[] { 2 }, ledger.AbsentPlayerIds(),
                "a resyncing seat is still auto-passed until the transfer completes");

            Assert.IsTrue(ledger.CompleteRejoin(2));
            Assert.AreEqual(ArenaSeatConnection.Connected, ledger.ConnectionOf(2));
            Assert.IsTrue(ledger.TryGetClientId(2, out var clientId));
            Assert.AreEqual(42UL, clientId, "the seat follows the new connection");
        }

        [Test]
        public void Rejoin_RefreshesGrace_ForALaterDrop()
        {
            var ledger = CreateSeededLedger();
            ledger.MarkDisconnected(2);
            ledger.TickRound();
            ledger.TickRound();

            ledger.TryBeginRejoin(ArenaRejoinToken.For(MatchSeed, 2), 42, 2);
            ledger.CompleteRejoin(2);

            // Dropping again starts a FULL grace, not the leftover of the first one.
            ledger.MarkDisconnected(2);
            for (int round = 0; round < GraceRounds; round++)
            {
                CollectionAssert.IsEmpty(ledger.TickRound());
            }

            CollectionAssert.AreEqual(new[] { 2 }, ledger.TickRound());
        }

        [Test]
        public void Rejoin_WithWrongToken_Rejected()
        {
            var ledger = CreateSeededLedger();
            ledger.MarkDisconnected(2);

            var wrongToken = ArenaRejoinToken.For(MatchSeed, 1);
            Assert.IsFalse(ledger.TryBeginRejoin(wrongToken, 42, 2));
            Assert.AreEqual(ArenaSeatConnection.Gracing, ledger.ConnectionOf(2));
        }

        [Test]
        public void Rejoin_OfConnectedOrDepartedSeat_Rejected()
        {
            var ledger = CreateSeededLedger();
            var token = ArenaRejoinToken.For(MatchSeed, 2);

            Assert.IsFalse(ledger.TryBeginRejoin(token, 42, 2), "a live seat cannot be claimed");

            ledger.MarkDisconnected(2);
            for (int round = 0; round <= GraceRounds; round++)
            {
                ledger.TickRound();
            }

            Assert.AreEqual(ArenaSeatConnection.Departed, ledger.ConnectionOf(2));
            Assert.IsFalse(ledger.TryBeginRejoin(token, 42, 2), "grace expired — the seat is gone");
        }

        [Test]
        public void RejoinToken_IsDeterministic_AndPerSeat()
        {
            Assert.AreEqual(
                ArenaRejoinToken.For(MatchSeed, 2),
                ArenaRejoinToken.For(MatchSeed, 2),
                "any peer holding the setup derives the same token");
            Assert.AreNotEqual(
                ArenaRejoinToken.For(MatchSeed, 1),
                ArenaRejoinToken.For(MatchSeed, 2));
            Assert.AreNotEqual(
                ArenaRejoinToken.For(MatchSeed, 2),
                ArenaRejoinToken.For(MatchSeed + 1, 2),
                "a different match yields different tokens");
        }

        [Test]
        public void MigrationSeed_StartsUnreachableSeatsInGrace()
        {
            var ledger = new ArenaSeatLedger();
            ledger.Seed(MatchSeed, Roster(), GraceRounds, new[] { 1, 3 });

            Assert.AreEqual(ArenaSeatConnection.Gracing, ledger.ConnectionOf(1));
            Assert.AreEqual(ArenaSeatConnection.Connected, ledger.ConnectionOf(2));
            Assert.AreEqual(ArenaSeatConnection.Gracing, ledger.ConnectionOf(3));
            CollectionAssert.AreEqual(new[] { 1, 3 }, ledger.AbsentPlayerIds());
        }

        [Test]
        public void ConnectPayload_RejoinClaim_RoundTrips()
        {
            var token = ArenaRejoinToken.For(MatchSeed, 2);
            var payload = ArenaConnectPayload.EncodeRejoin(2, token);

            Assert.IsTrue(ArenaConnectPayload.TryDecodeRejoin(payload, out var playerId, out var decoded));
            Assert.AreEqual(2, playerId);
            Assert.AreEqual(token, decoded);
        }

        [Test]
        public void ConnectPayload_GarbageOrEmpty_Rejected()
        {
            Assert.IsFalse(ArenaConnectPayload.TryDecodeRejoin(null, out _, out _));
            Assert.IsFalse(ArenaConnectPayload.TryDecodeRejoin(new byte[0], out _, out _));
            Assert.IsFalse(ArenaConnectPayload.TryDecodeRejoin(new byte[] { 9, 9, 9 }, out _, out _));

            var wrongMode = ArenaConnectPayload.EncodeRejoin(2, 123UL);
            wrongMode[1] = 0;
            Assert.IsFalse(ArenaConnectPayload.TryDecodeRejoin(wrongMode, out _, out _));
        }

        [Test]
        public void CommitCollector_MarkPassed_RemovesTheDebt_AndReinstateRestoresIt()
        {
            var collector = new ArenaCommitCollector();
            collector.BeginRound(1, new List<int> { 1, 2 });

            collector.MarkPassed(2);
            CollectionAssert.AreEqual(new[] { 2 }, collector.PassedPlayerIds());
            Assert.IsFalse(collector.AllCommitted, "player 1 still owes its commit");
            Assert.IsFalse(collector.TryAccept(1, new ArenaCommit(2, 2, Combat.Config.HexDirection.E,
                    new List<Combat.Core.EnemyIntent>())),
                "a passed seat no longer owes (or may submit) a commit");

            collector.Reinstate(2);
            CollectionAssert.IsEmpty(collector.PassedPlayerIds());
            Assert.IsTrue(collector.TryAccept(1, new ArenaCommit(2, 2, Combat.Config.HexDirection.E,
                new List<Combat.Core.EnemyIntent>())));
        }
    }
}
