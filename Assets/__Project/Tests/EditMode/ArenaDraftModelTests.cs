using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Pure-domain suite for the P4-5 draft state machine: snake order (req 8), pick legality,
    /// denial (req 9), completion (req 10), loadout correctness, and the deterministic
    /// auto-pick rule the host uses for timeouts / AI seats.
    /// </summary>
    [TestFixture]
    public class ArenaDraftModelTests
    {
        private static readonly string[] TwoSlots = { "slot.head", "slot.torso" };

        private static ArenaDraftBoardEntry Entry(int id, string partId, string slotId) =>
            new ArenaDraftBoardEntry(id, partId, slotId);

        /// <summary>A board with N copies per slot so every seat can always fill both slots.</summary>
        private static List<ArenaDraftBoardEntry> ViableBoard(int copiesPerSlot)
        {
            var board = new List<ArenaDraftBoardEntry>();
            int id = 1;
            foreach (var slot in TwoSlots)
            {
                for (int copy = 0; copy < copiesPerSlot; copy++)
                {
                    board.Add(Entry(id, $"part.{slot}.{copy}", slot));
                    id++;
                }
            }

            return board;
        }

        private static ArenaDraftModel TwoSeatModel() =>
            new ArenaDraftModel(ViableBoard(2), new[] { 1, 2 }, TwoSlots);

        // ---- snake order ----

        [Test]
        public void SnakeOrder_ThreeSeats_WrapsWithDoublePickAtTheTurn()
        {
            // …P1 P2 P3 | P3 P2 P1 | P1 P2 P3…
            var expectedSeatIndices = new[] { 0, 1, 2, 2, 1, 0, 0, 1, 2 };
            var actual = Enumerable.Range(0, 9)
                .Select(pick => ArenaSnakeOrder.SeatIndexAt(pick, 3))
                .ToArray();

            Assert.AreEqual(expectedSeatIndices, actual);
        }

        [Test]
        public void CurrentPicker_FollowsSnakeOrderAcrossTheFullDraft()
        {
            var model = TwoSeatModel();
            var pickers = new List<int>();

            while (!model.IsComplete)
            {
                int picker = model.CurrentPickerPlayerId;
                pickers.Add(picker);
                Apply(model, picker);
            }

            Assert.AreEqual(new[] { 1, 2, 2, 1 }, pickers.ToArray());
        }

        // ---- legality matrix ----

        [Test]
        public void TryApply_OutOfTurnPlayer_IsRejectedWithoutStateChange()
        {
            var model = TwoSeatModel();
            int entry = model.AutoPickEntryFor(2);

            var result = model.TryApply(new ArenaDraftPick(0, 2, entry, false));

            Assert.AreEqual(ArenaDraftPickResult.NotYourTurn, result);
            Assert.AreEqual(0, model.PickIndex);
            Assert.IsTrue(model.IsEntryAvailable(entry));
        }

        [Test]
        public void TryApply_StalePickIndex_IsRejected()
        {
            var model = TwoSeatModel();
            Apply(model, 1);

            var result = model.TryApply(new ArenaDraftPick(0, 2, model.AutoPickEntryFor(2), false));

            Assert.AreEqual(ArenaDraftPickResult.StalePickIndex, result);
        }

        [Test]
        public void TryApply_TakenEntry_IsRejected()
        {
            var model = TwoSeatModel();
            int taken = model.AutoPickEntryFor(1);
            Apply(model, 1, taken);

            var result = model.TryApply(new ArenaDraftPick(1, 2, taken, false));

            Assert.AreEqual(ArenaDraftPickResult.EntryTaken, result);
        }

        [Test]
        public void TryApply_SlotAlreadyFilled_IsRejected()
        {
            // P1 takes head copy 0; on P1's second pick (index 3) another head is illegal.
            var model = new ArenaDraftModel(ViableBoard(3), new[] { 1, 2 }, TwoSlots);
            Apply(model, 1, FirstAvailable(model, "slot.head"));
            Apply(model, 2);
            Apply(model, 2);

            var result = model.TryApply(
                new ArenaDraftPick(3, 1, FirstAvailable(model, "slot.head"), false));

            Assert.AreEqual(ArenaDraftPickResult.SlotAlreadyFilled, result);
        }

        [Test]
        public void TryApply_AfterCompletion_IsRejected()
        {
            var model = TwoSeatModel();
            RunToCompletion(model);

            var result = model.TryApply(new ArenaDraftPick(model.PickIndex, 1, 1, false));

            Assert.AreEqual(ArenaDraftPickResult.DraftComplete, result);
        }

        // ---- denial, completion, loadouts ----

        [Test]
        public void AppliedPick_RemovesTheEntryForEveryone()
        {
            var model = TwoSeatModel();
            int entry = model.AutoPickEntryFor(1);

            Apply(model, 1, entry);

            Assert.IsFalse(model.IsEntryAvailable(entry));
            Assert.IsFalse(model.AvailableEntries.Any(e => e.EntryId == entry));
        }

        [Test]
        public void Draft_CompletesAtSeatsTimesSlots_AndRaisesCompleted()
        {
            var model = TwoSeatModel();
            bool completed = false;
            model.Completed += () => completed = true;

            RunToCompletion(model);

            Assert.IsTrue(model.IsComplete);
            Assert.IsTrue(completed);
            Assert.AreEqual(4, model.PickIndex);
        }

        [Test]
        public void CompletedDraft_EverySeatHasEveryLoadoutSlotFilled()
        {
            var model = TwoSeatModel();
            RunToCompletion(model);

            foreach (var playerId in model.SeatOrder)
            {
                var loadout = model.LoadoutOf(playerId);
                Assert.AreEqual(TwoSlots.Length, loadout.Count, $"player {playerId}");
                foreach (var slot in TwoSlots)
                {
                    Assert.IsTrue(loadout.ContainsKey(slot), $"player {playerId} missing {slot}");
                }
            }
        }

        [Test]
        public void LoadoutOf_RecordsThePickedPartIdIntoTheEntrySlot()
        {
            var model = TwoSeatModel();
            var entry = model.Board.First();

            Apply(model, 1, entry.EntryId);

            Assert.AreEqual(entry.PartId, model.LoadoutOf(1)[entry.SlotId]);
        }

        // ---- auto-pick ----

        [Test]
        public void AutoPick_IsDeterministic_SameStateSameEntry()
        {
            var first = TwoSeatModel();
            var second = TwoSeatModel();

            Assert.AreEqual(first.AutoPickEntryFor(1), second.AutoPickEntryFor(1));
        }

        [Test]
        public void AutoPick_AlwaysFillsANeededSlot_AcrossAFullAutoDraft()
        {
            var model = new ArenaDraftModel(ViableBoard(3), new[] { 1, 2, 3 }, TwoSlots);

            while (!model.IsComplete)
            {
                int picker = model.CurrentPickerPlayerId;
                var before = model.LoadoutOf(picker).Count;
                Apply(model, picker);
                Assert.AreEqual(before + 1, model.LoadoutOf(picker).Count);
            }
        }

        [Test]
        public void AutoPick_PrefersEarlierLoadoutSlot_ThenLowestEntryId()
        {
            var board = new List<ArenaDraftBoardEntry>
            {
                Entry(1, "part.torso.x", "slot.torso"),
                Entry(2, "part.head.b", "slot.head"),
                Entry(3, "part.head.a", "slot.head"),
                Entry(4, "part.torso.y", "slot.torso")
            };
            var model = new ArenaDraftModel(board, new[] { 1, 2 }, TwoSlots);

            // slot.head comes first in the loadout; entry 2 is the lowest head entry id.
            Assert.AreEqual(2, model.AutoPickEntryFor(1));
        }

        [Test]
        public void AutoPick_WhenBoardCannotCoverOpenSlots_Throws()
        {
            var board = new List<ArenaDraftBoardEntry> { Entry(1, "part.head.a", "slot.head") };
            var model = new ArenaDraftModel(board, new[] { 1, 2 }, TwoSlots);
            Apply(model, 1, 1);

            Assert.Throws<InvalidOperationException>(() => model.AutoPickEntryFor(2));
        }

        // ---- helpers ----

        private static void Apply(ArenaDraftModel model, int playerId, int? entryId = null)
        {
            int entry = entryId ?? model.AutoPickEntryFor(playerId);
            var result = model.TryApply(new ArenaDraftPick(model.PickIndex, playerId, entry, false));
            Assert.AreEqual(ArenaDraftPickResult.Applied, result);
        }

        private static void RunToCompletion(ArenaDraftModel model)
        {
            while (!model.IsComplete)
            {
                Apply(model, model.CurrentPickerPlayerId);
            }
        }

        private static int FirstAvailable(ArenaDraftModel model, string slotId) =>
            model.AvailableEntries.Where(e => e.SlotId == slotId).OrderBy(e => e.EntryId).First().EntryId;
    }
}
