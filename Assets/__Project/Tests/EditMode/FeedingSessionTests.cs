using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FeedingSessionTests
    {
        private InventoryModel _inventory;
        private FeedingSession _session;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
            _session = new FeedingSession(_inventory);
        }

        [Test]
        public void Constructor_NullInventory_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new FeedingSession(null));
        }

        [Test]
        public void TrySelect_MovesItemFromInventoryToTrayAndRaisesChanged()
        {
            var fire = _inventory.Add("fire");
            var changed = 0;
            _session.OnTrayChanged += () => changed++;

            Assert.IsTrue(_session.TrySelect(fire.InstanceId));
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.AreEqual(1, _session.Tray.Count);
            Assert.AreSame(fire, _session.Tray[0]);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void TrySelect_UnknownInstance_ReturnsFalse()
        {
            Assert.IsFalse(_session.TrySelect(999));
            Assert.AreEqual(0, _session.Tray.Count);
        }

        [Test]
        public void TryUnselect_ReturnsItemToInventory()
        {
            var fire = _inventory.Add("fire");
            _session.TrySelect(fire.InstanceId);

            Assert.IsTrue(_session.TryUnselect(fire.InstanceId));
            Assert.AreEqual(0, _session.Tray.Count);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(fire, _inventory.Items[0]);
        }

        [Test]
        public void TryUnselect_NotInTray_ReturnsFalse()
        {
            Assert.IsFalse(_session.TryUnselect(123));
        }

        [Test]
        public void Consume_ClearsTrayWithoutReturningToInventory()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            var consumed = _session.Consume();

            Assert.AreEqual(2, consumed.Count);
            Assert.AreEqual(0, _session.Tray.Count);
            Assert.AreEqual(0, _inventory.Items.Count);
        }

        [Test]
        public void Consume_EmptyTray_ReturnsEmpty()
        {
            var consumed = _session.Consume();

            Assert.AreEqual(0, consumed.Count);
        }

        [Test]
        public void ReturnAll_ReturnsEveryTrayItemToInventory()
        {
            var fire = _inventory.Add("fire");
            var water = _inventory.Add("water");
            _session.TrySelect(fire.InstanceId);
            _session.TrySelect(water.InstanceId);

            _session.ReturnAll();

            Assert.AreEqual(0, _session.Tray.Count);
            Assert.AreEqual(2, _inventory.Items.Count);
        }
    }
}
