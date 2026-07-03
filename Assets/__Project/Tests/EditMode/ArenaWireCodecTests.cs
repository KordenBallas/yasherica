using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Networking;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Round-trips the lockstep wire format (domain → wire structs → domain): every step kind
    /// survives with its committed snapshot intact — cells, facing, origin, move destination —
    /// plus the envelope hash, bundle ordering/departures, and the match setup roster.
    /// (The NGO buffer layer itself is play-tested; these prove the mapping.)
    /// </summary>
    [TestFixture]
    public class ArenaWireCodecTests
    {
        private HumanPlayer _player;

        [SetUp]
        public void SetUp()
        {
            _player = new HumanPlayer(1, "P1");
        }

        private Combat.Core.IPlayer Resolve(int playerId) => _player;

        private static EnemyIntent AbilityIntent(IPlayer owner)
        {
            var action = new ScheduleAbilityAction(owner, 1, 100, HexDirection.W);
            var cells = new List<HexCoordinates> { new HexCoordinates(1, 0), new HexCoordinates(0, 0) };
            return new EnemyIntent(1, action, HexDirection.W, new HexCoordinates(2, 0), cells);
        }

        private static EnemyIntent MoveIntent(IPlayer owner)
        {
            var action = new MoveAction(owner, 1, new HexCoordinates(3, -1));
            return new EnemyIntent(1, action, null, new HexCoordinates(2, 0), null);
        }

        [Test]
        public void AbilityStep_RoundTrips_WithFrozenCellsFacingAndOrigin()
        {
            var original = AbilityIntent(_player);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original), _player);

            Assert.AreEqual(original.UnitId, decoded.UnitId);
            Assert.IsTrue(decoded.IsAbility);
            Assert.AreEqual(100, decoded.AbilityId);
            Assert.AreEqual(HexDirection.W, decoded.CommittedFacing);
            Assert.AreEqual(original.CommittedOrigin, decoded.CommittedOrigin);
            CollectionAssert.AreEqual(original.CommittedCells.ToList(), decoded.CommittedCells.ToList());
            Assert.AreEqual(HexDirection.W, ((ScheduleAbilityAction)decoded.Action).FacingToSet,
                "the resolver re-applies committed facing from the action too");
        }

        [Test]
        public void MoveStep_RoundTrips_WithDestination()
        {
            var original = MoveIntent(_player);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original), _player);

            Assert.IsTrue(decoded.IsMove);
            Assert.AreEqual(new HexCoordinates(3, -1), decoded.MoveDestination);
            Assert.IsNull(decoded.CommittedFacing);
            Assert.IsEmpty(decoded.CommittedCells.ToList());
        }

        [Test]
        public void CommitEnvelope_RoundTrips_WithHashAndStepOrder()
        {
            var commit = new ArenaCommit(1, 1, HexDirection.NE,
                new List<EnemyIntent> { AbilityIntent(_player), MoveIntent(_player) });
            var envelope = new ArenaCommitEnvelope(7, commit, 0xDEADBEEFCAFEBABEUL);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(envelope), Resolve);

            Assert.AreEqual(7, decoded.RoundNumber);
            Assert.AreEqual(0xDEADBEEFCAFEBABEUL, decoded.PreviousRoundStateHash);
            Assert.AreEqual(HexDirection.NE, decoded.Commit.FinalFacing);
            Assert.AreEqual(2, decoded.Commit.Steps.Count);
            Assert.IsTrue(decoded.Commit.Steps[0].IsAbility, "step order preserved");
            Assert.IsTrue(decoded.Commit.Steps[1].IsMove);
            Assert.AreSame(_player, decoded.Commit.Steps[0].Action.Player, "owner resolved by PlayerId");
        }

        [Test]
        public void RoundBundle_RoundTrips_WithCanonicalOrderAndDepartures()
        {
            var bundle = new ArenaRoundBundle(3,
                new List<ArenaCommit>
                {
                    new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>()),
                    new ArenaCommit(2, 2, HexDirection.W, new List<EnemyIntent> { MoveIntent(_player) })
                },
                new List<int> { 4 });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(bundle), Resolve);

            Assert.AreEqual(3, decoded.RoundNumber);
            Assert.AreEqual(new[] { 1, 2 }, decoded.Commits.Select(c => c.PlayerId).ToArray());
            Assert.AreEqual(new[] { 4 }, decoded.DepartedPlayerIds.ToArray());
            Assert.AreEqual(1, decoded.Commits[1].Steps.Count);
        }

        [Test]
        public void MatchSetup_RoundTrips_TheSeatAssignment()
        {
            var setup = new ArenaMatchSetup(12345, new List<ArenaRosterSlot>
            {
                new ArenaRosterSlot(0UL, 1, 1),
                new ArenaRosterSlot(3UL, 2, 2)
            });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(setup));

            Assert.AreEqual(12345, decoded.MatchSeed);
            Assert.AreEqual(2, decoded.Roster.Count);
            Assert.AreEqual(0UL, decoded.Roster[0].ClientId);
            Assert.AreEqual(2, decoded.Roster[1].PlayerId);
        }
    }
}
