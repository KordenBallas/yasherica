using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class InventoryModelTests
    {
        private InventoryModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new InventoryModel();
        }

        [Test]
        public void Add_ReturnsInstanceWithGivenDefinitionId()
        {
            var instance = _model.Add("fire");

            Assert.AreEqual("fire", instance.DefinitionId);
            Assert.AreEqual(1, _model.Items.Count);
            Assert.AreSame(instance, _model.Items[0]);
        }

        [Test]
        public void Add_GeneratesUniqueInstanceIds()
        {
            var first = _model.Add("fire");
            var second = _model.Add("fire");

            Assert.AreNotEqual(first.InstanceId, second.InstanceId);
        }

        [Test]
        public void Add_AllowsDuplicateDefinitions()
        {
            _model.Add("fire");
            _model.Add("fire");
            _model.Add("water");

            Assert.AreEqual(3, _model.Items.Count);
        }

        [Test]
        public void Remove_ByInstanceId_RemovesOnlyThatInstance()
        {
            var first = _model.Add("fire");
            var second = _model.Add("fire");

            bool removed = _model.Remove(first.InstanceId);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, _model.Items.Count);
            Assert.AreSame(second, _model.Items[0]);
        }

        [Test]
        public void Remove_UnknownInstanceId_ReturnsFalse()
        {
            _model.Add("fire");

            Assert.IsFalse(_model.Remove(999));
            Assert.AreEqual(1, _model.Items.Count);
        }

        [Test]
        public void Events_FireWithCorrectPayloads()
        {
            var added = new List<ArtifactInstance>();
            var removed = new List<ArtifactInstance>();
            _model.OnItemAdded += added.Add;
            _model.OnItemRemoved += removed.Add;

            var instance = _model.Add("rock");
            _model.Remove(instance.InstanceId);

            Assert.AreEqual(1, added.Count);
            Assert.AreSame(instance, added[0]);
            Assert.AreEqual(1, removed.Count);
            Assert.AreSame(instance, removed[0]);
        }

        [Test]
        public void CreateDetachedInstance_DoesNotAddToContainer()
        {
            var detached = _model.CreateDetachedInstance("snake");

            Assert.AreEqual(0, _model.Items.Count);
            Assert.AreEqual("snake", detached.DefinitionId);
        }

        [Test]
        public void CreateDetachedInstance_SharesIdSpaceWithAdd()
        {
            var added = _model.Add("fire");
            var detached = _model.CreateDetachedInstance("snake");

            Assert.AreNotEqual(added.InstanceId, detached.InstanceId);
        }

        [Test]
        public void Return_ReaddsExistingInstanceAndFiresAddedEvent()
        {
            var instance = _model.Add("fire");
            _model.Remove(instance.InstanceId);

            ArtifactInstance returnedPayload = null;
            _model.OnItemAdded += i => returnedPayload = i;
            _model.Return(instance);

            Assert.AreEqual(1, _model.Items.Count);
            Assert.AreSame(instance, returnedPayload);
        }

        [Test]
        public void Return_InstanceAlreadyInInventory_Throws()
        {
            var instance = _model.Add("fire");

            Assert.Throws<System.InvalidOperationException>(() => _model.Return(instance));
        }

        [Test]
        public void TryGet_FindsInstanceById()
        {
            var instance = _model.Add("virus");

            Assert.IsTrue(_model.TryGet(instance.InstanceId, out var found));
            Assert.AreSame(instance, found);
            Assert.IsFalse(_model.TryGet(999, out _));
        }
    }
}
