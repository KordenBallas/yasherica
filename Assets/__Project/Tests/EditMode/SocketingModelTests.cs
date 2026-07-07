using Inventory.Core;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class SocketingModelTests
    {
        private InventoryModel _inventory;
        private BlankRack _rack;
        private SocketingModel _socketing;
        private BlankInstance _skull; // 2 sockets

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
            _rack = new BlankRack(3);
            var blanks = new FakePartBlankDataSource()
                .Add("blank.skull", "slot.head", "reptile", 2)
                .Add("blank.arm", "slot.arm.left", "insect", 1);
            _socketing = new SocketingModel(_inventory, _rack, blanks);
            _rack.TryAdd("blank.skull", out _skull);
        }

        [Test]
        public void TrySocket_MovesArtifactFromInventoryIntoSocket()
        {
            var mace = _inventory.Add("mace");
            int changedBlank = -1;
            _socketing.OnSocketsChanged += id => changedBlank = id;

            Assert.IsTrue(_socketing.TrySocket(_skull.InstanceId, mace.InstanceId));
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.AreEqual(1, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreSame(mace, _socketing.SocketedArtifacts(_skull.InstanceId)[0]);
            Assert.AreEqual(_skull.InstanceId, changedBlank);
            Assert.IsFalse(_socketing.IsReady(_skull.InstanceId));
        }

        [Test]
        public void TrySocket_UnknownBlankOrArtifact_ReturnsFalse()
        {
            var mace = _inventory.Add("mace");

            Assert.IsFalse(_socketing.TrySocket(999, mace.InstanceId));
            Assert.IsFalse(_socketing.TrySocket(_skull.InstanceId, 999));
            Assert.AreEqual(1, _inventory.Items.Count);
        }

        [Test]
        public void TrySocket_FillingLastSocket_RaisesBlankReady()
        {
            var mace = _inventory.Add("mace");
            var stinger = _inventory.Add("stinger");
            int readyBlank = -1;
            _socketing.OnBlankReady += id => readyBlank = id;

            _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);
            Assert.AreEqual(-1, readyBlank);

            _socketing.TrySocket(_skull.InstanceId, stinger.InstanceId);
            Assert.AreEqual(_skull.InstanceId, readyBlank);
            Assert.IsTrue(_socketing.IsReady(_skull.InstanceId));
        }

        [Test]
        public void TrySocket_FullBlank_IsRejected()
        {
            var a = _inventory.Add("a");
            var b = _inventory.Add("b");
            var c = _inventory.Add("c");
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, b.InstanceId);

            Assert.IsFalse(_socketing.TrySocket(_skull.InstanceId, c.InstanceId));
            Assert.AreEqual(1, _inventory.Items.Count);
        }

        [Test]
        public void TryUnsocket_ReturnsArtifactToInventory_WhileNotFull()
        {
            var mace = _inventory.Add("mace");
            _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);

            Assert.IsTrue(_socketing.TryUnsocket(_skull.InstanceId, mace.InstanceId));
            Assert.AreEqual(0, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreSame(mace, _inventory.Items[0]);
        }

        [Test]
        public void TryUnsocket_FullBlank_ReopensIt_TheCommitIsTheUnsealConfirm()
        {
            // Track F medallion beat: re-slotting stays free until the player
            // confirms the unseal, so a ready blank simply reopens.
            var a = _inventory.Add("a");
            var b = _inventory.Add("b");
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, b.InstanceId);
            Assert.IsTrue(_socketing.IsReady(_skull.InstanceId));

            Assert.IsTrue(_socketing.TryUnsocket(_skull.InstanceId, a.InstanceId));
            Assert.AreEqual(1, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.IsFalse(_socketing.IsReady(_skull.InstanceId));
        }

        [Test]
        public void TrySocket_RefillingAReopenedBlank_RaisesBlankReadyAgain()
        {
            var a = _inventory.Add("a");
            var b = _inventory.Add("b");
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, b.InstanceId);
            _socketing.TryUnsocket(_skull.InstanceId, a.InstanceId);

            int readyBlank = -1;
            _socketing.OnBlankReady += id => readyBlank = id;
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);

            Assert.AreEqual(_skull.InstanceId, readyBlank);
            Assert.IsTrue(_socketing.IsReady(_skull.InstanceId));
        }

        [Test]
        public void TryUnsocket_UnknownArtifact_ReturnsFalse()
        {
            Assert.IsFalse(_socketing.TryUnsocket(_skull.InstanceId, 999));
        }

        [Test]
        public void ConsumeSockets_DestroysArtifacts_WithoutReturningThem()
        {
            var a = _inventory.Add("a");
            var b = _inventory.Add("b");
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, b.InstanceId);

            var consumed = _socketing.ConsumeSockets(_skull.InstanceId);

            Assert.AreEqual(2, consumed.Count);
            Assert.AreEqual(0, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.IsFalse(_socketing.IsReady(_skull.InstanceId));
        }

        [Test]
        public void ConsumeSockets_EmptyBlank_YieldsNothing()
        {
            Assert.AreEqual(0, _socketing.ConsumeSockets(_skull.InstanceId).Count);
        }

        [Test]
        public void ReturnAll_ReturnsEverySocketedArtifact()
        {
            _rack.TryAdd("blank.arm", out var arm);
            var a = _inventory.Add("a");
            var b = _inventory.Add("b");
            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(arm.InstanceId, b.InstanceId);

            _socketing.ReturnAll();

            Assert.AreEqual(2, _inventory.Items.Count);
            Assert.AreEqual(0, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(0, _socketing.SocketedArtifacts(arm.InstanceId).Count);
        }

        [Test]
        public void SingleSocketBlank_IsReadyAfterOneArtifact()
        {
            _rack.TryAdd("blank.arm", out var arm);
            var a = _inventory.Add("a");
            int readyBlank = -1;
            _socketing.OnBlankReady += id => readyBlank = id;

            _socketing.TrySocket(arm.InstanceId, a.InstanceId);

            Assert.AreEqual(arm.InstanceId, readyBlank);
            Assert.IsTrue(_socketing.IsReady(arm.InstanceId));
        }
    }
}
