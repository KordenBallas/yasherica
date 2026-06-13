using System;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CharacterAssemblyStateTests
    {
        private CharacterAssemblyState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new CharacterAssemblyState();
        }

        private static PartData Part(string partId, string slotId)
        {
            return new PartData(partId, slotId, "skeleton.placeholder", new[] { "Root" }, Array.Empty<SocketInfo>());
        }

        [Test]
        public void Equip_IntoEmptySlot_ReturnsNullPrevious()
        {
            var previous = _state.Equip(Part("part.head.a", "slot.head"));

            Assert.IsNull(previous);
            Assert.IsTrue(_state.TryGetEquipped("slot.head", out var equipped));
            Assert.AreEqual("part.head.a", equipped.PartId);
        }

        [Test]
        public void Equip_IntoOccupiedSlot_ReturnsReplacedPart()
        {
            _state.Equip(Part("part.head.a", "slot.head"));

            var previous = _state.Equip(Part("part.head.b", "slot.head"));

            Assert.AreEqual("part.head.a", previous.PartId);
            _state.TryGetEquipped("slot.head", out var equipped);
            Assert.AreEqual("part.head.b", equipped.PartId);
        }

        [Test]
        public void Equip_UnknownSlotId_CreatesSlotEntry()
        {
            // Slots are data-driven/open: equipping into a never-seen slot id must work.
            _state.Equip(Part("part.tail", "slot.tail"));

            Assert.IsTrue(_state.TryGetEquipped("slot.tail", out _));
            Assert.AreEqual(1, _state.EquippedParts.Count);
        }

        [Test]
        public void Equip_RaisesEventsWithCorrectPayloads()
        {
            PartData equippedPayload = null;
            PartData removedPayload = null;
            _state.PartEquipped += p => equippedPayload = p;
            _state.PartRemoved += p => removedPayload = p;

            _state.Equip(Part("part.head.a", "slot.head"));
            Assert.AreEqual("part.head.a", equippedPayload.PartId);
            Assert.IsNull(removedPayload);

            _state.Equip(Part("part.head.b", "slot.head"));
            Assert.AreEqual("part.head.b", equippedPayload.PartId);
            Assert.AreEqual("part.head.a", removedPayload.PartId);
        }

        [Test]
        public void Remove_OccupiedSlot_ClearsAndReturnsPart()
        {
            _state.Equip(Part("part.head.a", "slot.head"));

            var removed = _state.Remove("slot.head");

            Assert.AreEqual("part.head.a", removed.PartId);
            Assert.IsFalse(_state.TryGetEquipped("slot.head", out _));
        }

        [Test]
        public void Remove_EmptySlot_ReturnsNullWithoutEvent()
        {
            var removedEventFired = false;
            _state.PartRemoved += _ => removedEventFired = true;

            var removed = _state.Remove("slot.head");

            Assert.IsNull(removed);
            Assert.IsFalse(removedEventFired);
        }
    }
}
