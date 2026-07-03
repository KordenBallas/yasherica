using Combat.Battlefield;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class UnitGroundingTests
    {
        [Test]
        public void FeetOffset_CenteredCapsule_IsHalfHeight()
        {
            // Hero/Enemy prefabs: capsule height 2, center y 0, scale 1 → pivot sits 1.0 above feet.
            Assert.AreEqual(1f, UnitGrounding.FeetOffset(colliderHeight: 2f, colliderCenterY: 0f, scaleY: 1f), 1e-5f);
        }

        [Test]
        public void FeetOffset_RaisedColliderCenter_ShrinksTheOffset()
        {
            // Collider lifted by 0.5 → its bottom is only 0.5 below the pivot.
            Assert.AreEqual(0.5f, UnitGrounding.FeetOffset(colliderHeight: 2f, colliderCenterY: 0.5f, scaleY: 1f), 1e-5f);
        }

        [Test]
        public void FeetOffset_FeetPivot_IsZero()
        {
            // A model authored with its pivot at the feet (collider center at half height) needs no lift.
            Assert.AreEqual(0f, UnitGrounding.FeetOffset(colliderHeight: 2f, colliderCenterY: 1f, scaleY: 1f), 1e-5f);
        }

        [Test]
        public void FeetOffset_ScalesWithWorldScale()
        {
            Assert.AreEqual(2f, UnitGrounding.FeetOffset(colliderHeight: 2f, colliderCenterY: 0f, scaleY: 2f), 1e-5f);
        }

        [Test]
        public void Grounded_AddsExactlyTheUpOffset()
        {
            var cellTop = new Vector3(25f, 3f, -4f);
            Vector3 grounded = UnitGrounding.Grounded(cellTop, 1f);
            Assert.AreEqual(cellTop.x, grounded.x, 1e-5f);
            Assert.AreEqual(cellTop.y + 1f, grounded.y, 1e-5f);
            Assert.AreEqual(cellTop.z, grounded.z, 1e-5f);
        }
    }
}
