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

        // ---- X1 reconnect / resync / migration ----

        [Test]
        public void RoundBundle_RoundTrips_AutoPassedPlayers()
        {
            var bundle = new ArenaRoundBundle(5,
                new List<ArenaCommit> { new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>()) },
                new List<int> { 4 },
                new List<int> { 2, 3 });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(bundle), Resolve);

            Assert.AreEqual(new[] { 2, 3 }, decoded.AutoPassedPlayerIds.ToArray());
            Assert.AreEqual(new[] { 4 }, decoded.DepartedPlayerIds.ToArray());
        }

        private static ArenaStateSnapshot SampleSnapshot()
        {
            return new ArenaStateSnapshot(ArenaStateSnapshot.CurrentVersion, 6, 0xFEEDF00DUL,
                new List<ArenaUnitSnapshot>
                {
                    new ArenaUnitSnapshot(1, 1, 2, -1, 17, 30, (int)HexDirection.W,
                        new List<ArenaAbilityCooldownSnapshot>
                        {
                            new ArenaAbilityCooldownSnapshot(100, 2),
                            new ArenaAbilityCooldownSnapshot(101, 0)
                        },
                        new List<ArenaStatusSnapshot> { new ArenaStatusSnapshot(2, 3, 2) }),
                    new ArenaUnitSnapshot(2, 2, 4, 0, 0, 30, (int)HexDirection.E,
                        new List<ArenaAbilityCooldownSnapshot>(),
                        new List<ArenaStatusSnapshot>())
                });
        }

        [Test]
        public void StateSnapshot_RoundTrips_UnitsCooldownsAndStatuses()
        {
            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(SampleSnapshot()));

            Assert.AreEqual(ArenaStateSnapshot.CurrentVersion, decoded.Version);
            Assert.AreEqual(6, decoded.RoundNumber);
            Assert.AreEqual(0xFEEDF00DUL, decoded.LastRoundHash);
            Assert.AreEqual(2, decoded.Units.Count);

            var unit1 = decoded.Units[0];
            Assert.AreEqual(1, unit1.UnitId);
            Assert.AreEqual(2, unit1.Q);
            Assert.AreEqual(-1, unit1.R);
            Assert.AreEqual(17, unit1.CurrentHP);
            Assert.AreEqual((int)HexDirection.W, unit1.Facing);
            Assert.AreEqual(2, unit1.Abilities.Count);
            Assert.AreEqual(2, unit1.Abilities[0].CurrentCooldown);
            Assert.AreEqual(1, unit1.Statuses.Count);
            Assert.AreEqual(2, unit1.Statuses[0].EffectId);
            Assert.AreEqual(3, unit1.Statuses[0].Duration);
            Assert.AreEqual(2, unit1.Statuses[0].StackCount);

            Assert.AreEqual(0, decoded.Units[1].CurrentHP, "a dead unit stays dead on the wire");
        }

        [Test]
        public void RejoinPackage_RoundTrips_WithLoadoutsAndReplayedBundle()
        {
            var setup = new ArenaMatchSetup(777, new List<ArenaRosterSlot>
            {
                new ArenaRosterSlot(0UL, 1, 1),
                new ArenaRosterSlot(9UL, 2, 2)
            });
            var loadouts = new Dictionary<int, IReadOnlyDictionary<string, string>>
            {
                [1] = new Dictionary<string, string> { ["arm-left"] = "part.claw", ["tail"] = "part.stinger" },
                [2] = new Dictionary<string, string> { ["arm-left"] = "part.hammer" }
            };
            var bundle = new ArenaRoundBundle(6,
                new List<ArenaCommit> { new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent> { MoveIntent(_player) }) },
                autoPassedPlayerIds: new List<int> { 2 });
            var package = new ArenaRejoinPackage(2, setup, loadouts, SampleSnapshot(), new List<int> { 3 }, bundle);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(package), Resolve);

            Assert.AreEqual(2, decoded.TargetPlayerId);
            Assert.AreEqual(777, decoded.Setup.MatchSeed);
            Assert.AreEqual("part.stinger", decoded.LoadoutByPlayerId[1]["tail"]);
            Assert.AreEqual("part.hammer", decoded.LoadoutByPlayerId[2]["arm-left"]);
            Assert.AreEqual(6, decoded.Snapshot.RoundNumber);
            Assert.AreEqual(new[] { 3 }, decoded.DepartedPlayerIds.ToArray());
            Assert.IsNotNull(decoded.CurrentRoundBundleOrNull);
            Assert.AreEqual(6, decoded.CurrentRoundBundleOrNull.RoundNumber);
            Assert.AreEqual(new[] { 2 }, decoded.CurrentRoundBundleOrNull.AutoPassedPlayerIds.ToArray());
        }

        [Test]
        public void RejoinPackage_WithoutBundle_RoundTripsNull()
        {
            var setup = new ArenaMatchSetup(777, new List<ArenaRosterSlot> { new ArenaRosterSlot(0UL, 1, 1) });
            var package = new ArenaRejoinPackage(
                1, setup, null, SampleSnapshot(), null, currentRoundBundleOrNull: null);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(package), Resolve);

            Assert.IsNull(decoded.CurrentRoundBundleOrNull);
        }

        [Test]
        public void ResyncCommandAndAck_RoundTrip()
        {
            var command = ArenaWireCodec.FromWire(
                ArenaWireCodec.ToWire(new ArenaResyncCommand(3, SampleSnapshot())));
            Assert.AreEqual(3, command.TargetPlayerId);
            Assert.AreEqual(6, command.Snapshot.RoundNumber);

            var ack = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(new ArenaResyncAck(3, 6)));
            Assert.AreEqual(3, ack.PlayerId);
            Assert.AreEqual(6, ack.RoundNumber);
        }

        [Test]
        public void AddressBook_RoundTrips_Endpoints()
        {
            var book = new ArenaAddressBook(new List<ArenaEndpoint>
            {
                new ArenaEndpoint(1, "192.168.1.10:7777"),
                new ArenaEndpoint(2, "192.168.1.20")
            });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(book));

            Assert.AreEqual(2, decoded.Endpoints.Count);
            Assert.AreEqual("192.168.1.10:7777", decoded.Endpoints[0].Address);
            Assert.AreEqual(2, decoded.Endpoints[1].PlayerId);
        }
    }
}
