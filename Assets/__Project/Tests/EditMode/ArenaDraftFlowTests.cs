using System.Collections.Generic;
using System.Linq;
using Combat.Arena;
using Combat.Arena.Core;
using Combat.Core;
using Combat.Player;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The draft's full application flow over the loopback transport, production wiring: the
    /// host authority (ArenaDraftHost) composes/broadcasts and enforces deadlines through a
    /// fake clock; the replica (ArenaDraftFlow) advances only on applied broadcasts. Covers a
    /// complete human+AI draft, timeout auto-pick, out-of-turn rejection, mid-draft departure,
    /// and host/replica loadout agreement.
    /// </summary>
    [TestFixture]
    public class ArenaDraftFlowTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Errors = new List<string>();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) => Errors.Add(message);
        }

        private sealed class FakeClock : IArenaDraftClock
        {
            public float Now { get; set; }
        }

        private sealed class FakePartInfoSource : IArenaDraftPartInfoSource
        {
            private readonly Dictionary<string, string> _slotByPartId;

            public FakePartInfoSource(Dictionary<string, string> slotByPartId)
            {
                _slotByPartId = slotByPartId;
            }

            public bool TryGet(string partId, out ArenaDraftPartInfo info)
            {
                if (_slotByPartId.TryGetValue(partId, out var slotId))
                {
                    info = new ArenaDraftPartInfo(partId, slotId);
                    return true;
                }

                info = null;
                return false;
            }
        }

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private static readonly string[] Slots = { "slot.head", "slot.torso" };
        private const int MatchSeed = 42;
        private const float HumanTimer = 45f;
        private const float AiDelay = 1.5f;

        private LoopbackArenaTransport _transport;
        private FakeClock _clock;
        private FakeLogger _logger;
        private ArenaTastedCatalogRegistry _registry;
        private ArenaDraftHost _host;
        private ArenaDraftFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _transport = new LoopbackArenaTransport();
            _clock = new FakeClock();
            _logger = new FakeLogger();
            _registry = new ArenaTastedCatalogRegistry(_transport);

            var settings = new ArenaDraftSettings(
                Slots,
                FloorFor(copiesPerSlot: 3),
                catalogSampleSize: 2,
                pickTimerSeconds: HumanTimer,
                aiPickDelaySeconds: AiDelay);

            _host = new ArenaDraftHost(
                _transport, _registry, new FakePartInfoSource(new Dictionary<string, string>
                {
                    ["part.head.tasted"] = "slot.head",
                    ["part.torso.tasted"] = "slot.torso"
                }),
                settings, _clock, _logger);

            _flow = new ArenaDraftFlow(_transport, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _flow.Dispose();
            _host.Dispose();
            _registry.Dispose();
        }

        private static List<ArenaDraftPartInfo> FloorFor(int copiesPerSlot)
        {
            var floor = new List<ArenaDraftPartInfo>();
            foreach (var slot in Slots)
            {
                for (int copy = 0; copy < copiesPerSlot; copy++)
                {
                    floor.Add(new ArenaDraftPartInfo($"part.{slot}.floor{copy}", slot));
                }
            }

            return floor;
        }

        private static List<IPlayer> ThreeSeats() => new List<IPlayer>
        {
            new HumanPlayer(1, "Player 1"),
            new AIPlayer(2, "Dummy 2", new NoopAI()),
            new AIPlayer(3, "Dummy 3", new NoopAI())
        };

        private static IReadOnlyList<ArenaRosterSlot> RosterOf(IReadOnlyList<IPlayer> players) =>
            players.Select(p => new ArenaRosterSlot(0, p.Id, p.Id)).ToList();

        private void StartDraft(IReadOnlyList<IPlayer> players)
        {
            _flow.PrepareForDraft(players);
            _transport.SubmitTastedCatalog(new List<string> { "part.head.tasted", "part.torso.tasted" });
            _host.StartDraft(
                MatchSeed,
                RosterOf(players),
                players.Where(p => p is AIPlayer).Select(p => p.Id).ToList());
        }

        /// <summary>Advances the fake clock past the current deadline and ticks the host once.</summary>
        private void ElapseAndTick(float seconds)
        {
            _clock.Now += seconds;
            _host.Tick();
        }

        private void PickAsHuman(int playerId)
        {
            var model = _flow.Model;
            _transport.SubmitDraftPick(new ArenaDraftPick(
                model.PickIndex, playerId, model.AutoPickEntryFor(playerId), false));
        }

        // ---- full draft ----

        [Test]
        public void FullDraft_HumanPicksAndAiAutoPicks_RunsToCompletion()
        {
            var players = ThreeSeats();
            bool completed = false;
            StartDraft(players);
            _flow.DraftCompleted += () => completed = true;

            // Snake over 3 seats × 2 slots: 1,2,3 | 3,2,1. AI seats fill on the short delay.
            PickAsHuman(1);
            ElapseAndTick(AiDelay + 0.1f); // P2
            ElapseAndTick(AiDelay + 0.1f); // P3
            ElapseAndTick(AiDelay + 0.1f); // P3 again (snake turn)
            ElapseAndTick(AiDelay + 0.1f); // P2
            PickAsHuman(1);

            Assert.IsTrue(completed);
            Assert.IsTrue(_flow.Model.IsComplete);
            foreach (var player in players)
            {
                Assert.AreEqual(Slots.Length, _flow.Model.LoadoutOf(player.Id).Count,
                    $"player {player.Id} must fill every slot");
            }

            Assert.IsEmpty(_logger.Errors, "no replica divergence");
        }

        [Test]
        public void DraftOpened_ReplicaBoardContainsFloorAndSampledCatalog()
        {
            StartDraft(ThreeSeats());

            // Floor 3×2 + the two tasted (distinct, draftable) catalog parts.
            Assert.AreEqual(8, _flow.Model.Board.Count);
            Assert.IsTrue(_flow.Model.Board.Any(e => e.PartId == "part.head.tasted"));
            Assert.IsTrue(_flow.Model.Board.Any(e => e.PartId == "part.torso.tasted"));
        }

        [Test]
        public void CompletedDraft_ConfirmDeliversResult_MatchingHostLoadouts()
        {
            StartDraft(ThreeSeats());
            ArenaDraftResult result = null;
            _flow.ReadyForCombat += r => result = r;

            PickAsHuman(1);
            for (int i = 0; i < 4; i++)
            {
                ElapseAndTick(AiDelay + 0.1f);
            }

            PickAsHuman(1);
            _flow.ConfirmCompletion();

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.LoadoutByPlayerId.Count);
            foreach (var playerId in _flow.Model.SeatOrder)
            {
                CollectionAssert.AreEquivalent(
                    _flow.Model.LoadoutOf(playerId).Values,
                    result.LoadoutByPlayerId[playerId].Values);
            }

            Assert.IsEmpty(result.DepartedPlayerIds);
        }

        // ---- authority & legality ----

        [Test]
        public void OutOfTurnRequest_IsRejectedByTheHost_NoBroadcastNoStateChange()
        {
            StartDraft(ThreeSeats());
            int picksSeen = 0;
            _flow.PickApplied += _ => picksSeen++;

            // Player 2 tries to jump player 1's opening pick.
            var model = _flow.Model;
            _transport.SubmitDraftPick(new ArenaDraftPick(
                model.PickIndex, 2, model.AutoPickEntryFor(2), false));

            Assert.AreEqual(0, picksSeen);
            Assert.AreEqual(0, _flow.Model.PickIndex);
        }

        [Test]
        public void HumanTimeout_AutoPicksALegalPart_MarkedAsAutoPick()
        {
            StartDraft(ThreeSeats());
            ArenaDraftPick lastPick = null;
            _flow.PickApplied += pick => lastPick = pick;

            ElapseAndTick(HumanTimer + 0.1f);

            Assert.IsNotNull(lastPick);
            Assert.AreEqual(1, lastPick.PlayerId);
            Assert.IsTrue(lastPick.WasAutoPick);
            Assert.AreEqual(1, _flow.Model.LoadoutOf(1).Count);
        }

        [Test]
        public void BeforeTimeout_TickDoesNothing()
        {
            StartDraft(ThreeSeats());

            ElapseAndTick(HumanTimer - 1f);

            Assert.AreEqual(0, _flow.Model.PickIndex);
        }

        // ---- departure ----

        [Test]
        public void MidDraftDeparture_SeatAutoFills_AndDepartureReachesTheResult()
        {
            StartDraft(ThreeSeats());
            ArenaDraftResult result = null;
            _flow.ReadyForCombat += r => result = r;

            PickAsHuman(1);
            _transport.SimulateDeparture(2); // P2 vanishes; it is P2's turn — fills immediately.
            _host.Tick();                    // departed deadline is "now"
            ElapseAndTick(AiDelay + 0.1f);   // P3
            ElapseAndTick(AiDelay + 0.1f);   // P3 (snake)
            _host.Tick();                    // P2 departed — immediate
            PickAsHuman(1);
            _flow.ConfirmCompletion();

            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new[] { 2 }, result.DepartedPlayerIds.ToList());
            Assert.AreEqual(Slots.Length, result.LoadoutByPlayerId[2].Count,
                "the departed seat still drafts to a complete body");
        }

        // ---- determinism ----

        [Test]
        public void SameSeedAndCatalogs_TwoIndependentDrafts_ProduceIdenticalBoards()
        {
            StartDraft(ThreeSeats());
            var firstBoard = _flow.Model.Board
                .Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList();

            // Rebuild the whole stack from scratch with the same inputs.
            TearDown();
            SetUp();
            StartDraft(ThreeSeats());

            CollectionAssert.AreEqual(
                firstBoard,
                _flow.Model.Board.Select(e => (e.EntryId, e.PartId, e.SlotId)).ToList());
        }
    }
}
