using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BlankRackTests
    {
        [Test]
        public void Constructor_CapacityBelowOne_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BlankRack(0));
        }

        [Test]
        public void TryAdd_CreatesInstanceWithUniqueIds_AndRaisesChanged()
        {
            var rack = new BlankRack(3);
            int changed = 0;
            rack.OnChanged += () => changed++;

            Assert.IsTrue(rack.TryAdd("blank.skull", out var first));
            Assert.IsTrue(rack.TryAdd("blank.skull", out var second));

            Assert.AreEqual("blank.skull", first.DefinitionId);
            Assert.AreNotEqual(first.InstanceId, second.InstanceId);
            Assert.AreEqual(2, rack.Blanks.Count);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void TryAdd_AtCapacity_IsRejected()
        {
            var rack = new BlankRack(1);
            rack.TryAdd("blank.a", out _);

            Assert.IsFalse(rack.TryAdd("blank.b", out var rejected));
            Assert.IsNull(rejected);
            Assert.AreEqual(1, rack.Blanks.Count);
        }

        [Test]
        public void TryAdd_EmptyDefinitionId_IsRejected()
        {
            var rack = new BlankRack(1);

            Assert.IsFalse(rack.TryAdd("", out _));
            Assert.IsFalse(rack.TryAdd(null, out _));
        }

        [Test]
        public void Remove_FreesCapacity_AndRaisesChanged()
        {
            var rack = new BlankRack(1);
            rack.TryAdd("blank.a", out var instance);
            int changed = 0;
            rack.OnChanged += () => changed++;

            Assert.IsTrue(rack.Remove(instance.InstanceId));
            Assert.AreEqual(0, rack.Blanks.Count);
            Assert.AreEqual(1, changed);
            Assert.IsTrue(rack.TryAdd("blank.b", out _));
        }

        [Test]
        public void Remove_UnknownId_ReturnsFalse()
        {
            var rack = new BlankRack(1);

            Assert.IsFalse(rack.Remove(42));
        }

        [Test]
        public void TryGet_FindsRackedInstance()
        {
            var rack = new BlankRack(2);
            rack.TryAdd("blank.a", out var instance);

            Assert.IsTrue(rack.TryGet(instance.InstanceId, out var found));
            Assert.AreSame(instance, found);
            Assert.IsFalse(rack.TryGet(999, out _));
        }
    }
}
