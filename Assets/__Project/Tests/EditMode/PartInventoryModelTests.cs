using System;
using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PartInventoryModelTests
    {
        private PartInventoryModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new PartInventoryModel();
        }

        [Test]
        public void Add_StoresPartIdInInsertionOrder()
        {
            _model.Add("part.leg.l");
            _model.Add("part.leg.r");

            CollectionAssert.AreEqual(new[] { "part.leg.l", "part.leg.r" }, _model.PartIds);
        }

        [Test]
        public void Add_DuplicateIds_Accumulate()
        {
            _model.Add("part.leg.l");
            _model.Add("part.leg.l");

            Assert.AreEqual(2, _model.PartIds.Count);
        }

        [Test]
        public void Add_RaisesEventWithPartId()
        {
            var received = new List<string>();
            _model.OnPartAdded += received.Add;

            _model.Add("part.leg.l");

            CollectionAssert.AreEqual(new[] { "part.leg.l" }, received);
        }

        [Test]
        public void Add_NullOrEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => _model.Add(null));
            Assert.Throws<ArgumentException>(() => _model.Add(string.Empty));
        }

        [Test]
        public void PartIds_EmptyByDefault()
        {
            Assert.AreEqual(0, _model.PartIds.Count);
        }
    }
}
