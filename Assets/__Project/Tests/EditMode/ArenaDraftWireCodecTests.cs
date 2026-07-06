using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Arena.Networking;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Round-trips the draft wire format (domain → wire structs → domain): tasted catalogs,
    /// the draft-start handshake (board + loadout + timer), pick requests, and applied picks
    /// with piggybacked departures. (The NGO buffer layer is play-tested; these prove the mapping.)
    /// </summary>
    [TestFixture]
    public class ArenaDraftWireCodecTests
    {
        [Test]
        public void TastedCatalog_RoundTrips()
        {
            var original = new List<string> { "part.head.a", "part.tail.a" };

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original));

            CollectionAssert.AreEqual(original, decoded.ToList());
        }

        [Test]
        public void TastedCatalog_Empty_RoundTrips()
        {
            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(new List<string>()));

            Assert.IsEmpty(decoded);
        }

        [Test]
        public void DraftStart_RoundTrips_BoardLoadoutAndTimer()
        {
            var original = new ArenaDraftStart(
                45f,
                new List<string> { "slot.head", "slot.torso" },
                new List<ArenaDraftBoardEntry>
                {
                    new ArenaDraftBoardEntry(1, "part.head.a", "slot.head"),
                    new ArenaDraftBoardEntry(2, "part.torso.a", "slot.torso")
                });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original));

            Assert.AreEqual(45f, decoded.PickTimerSeconds);
            CollectionAssert.AreEqual(original.SlotLoadout.ToList(), decoded.SlotLoadout.ToList());
            Assert.AreEqual(2, decoded.Board.Count);
            Assert.AreEqual(1, decoded.Board[0].EntryId);
            Assert.AreEqual("part.head.a", decoded.Board[0].PartId);
            Assert.AreEqual("slot.head", decoded.Board[0].SlotId);
        }

        [Test]
        public void DraftStart_EmptyBoard_RoundTrips()
        {
            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(
                new ArenaDraftStart(10f, new List<string>(), new List<ArenaDraftBoardEntry>())));

            Assert.IsEmpty(decoded.Board);
            Assert.IsEmpty(decoded.SlotLoadout);
        }

        [Test]
        public void DraftPick_RoundTrips_IncludingAutoPickFlag()
        {
            var original = new ArenaDraftPick(3, 2, 17, wasAutoPick: true);

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original));

            Assert.AreEqual(3, decoded.PickIndex);
            Assert.AreEqual(2, decoded.PlayerId);
            Assert.AreEqual(17, decoded.EntryId);
            Assert.IsTrue(decoded.WasAutoPick);
        }

        [Test]
        public void DraftPickApplied_RoundTrips_WithDepartures()
        {
            var original = new ArenaDraftPickApplied(
                new ArenaDraftPick(5, 1, 9, false), new List<int> { 3 });

            var decoded = ArenaWireCodec.FromWire(ArenaWireCodec.ToWire(original));

            Assert.AreEqual(5, decoded.Pick.PickIndex);
            Assert.AreEqual(9, decoded.Pick.EntryId);
            Assert.IsFalse(decoded.Pick.WasAutoPick);
            CollectionAssert.AreEqual(new[] { 3 }, decoded.DepartedPlayerIds.ToList());
        }
    }
}
