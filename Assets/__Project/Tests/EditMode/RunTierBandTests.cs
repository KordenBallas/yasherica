using Narrative.Director.Core;
using Narrative.Director.Data;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunTierBandTests
    {
        [Test]
        public void DefaultBand_IsOpenAtEveryTier()
        {
            // The unbanded default (0,0) — what an unauthored field or an older asset deserialises to —
            // must stay eligible at every altitude so existing content needs no migration.
            var any = default(RunTierBand);
            Assert.IsTrue(any.Contains(0));
            Assert.IsTrue(any.Contains(1));
            Assert.IsTrue(any.Contains(99));
            Assert.IsTrue(RunTierBand.Any.Contains(5));
        }

        [Test]
        public void MinTier_OpensUpward_WithNoUpperBound()
        {
            var courtsAndUp = new RunTierBand(3, 0);   // max <= 0 = no ceiling
            Assert.IsFalse(courtsAndUp.Contains(1));
            Assert.IsFalse(courtsAndUp.Contains(2));
            Assert.IsTrue(courtsAndUp.Contains(3));
            Assert.IsTrue(courtsAndUp.Contains(4));
            Assert.IsTrue(courtsAndUp.Contains(99));
        }

        [Test]
        public void MaxTier_ClosesTheBandFromAbove()
        {
            var backwaterOnly = new RunTierBand(1, 1);
            Assert.IsFalse(backwaterOnly.Contains(0));
            Assert.IsTrue(backwaterOnly.Contains(1));
            Assert.IsFalse(backwaterOnly.Contains(2));

            var courts = new RunTierBand(3, 4);
            Assert.IsFalse(courts.Contains(2));
            Assert.IsTrue(courts.Contains(3));
            Assert.IsTrue(courts.Contains(4));
            Assert.IsFalse(courts.Contains(5));
        }

        [Test]
        public void Authoring_ToCore_RoundTripsMinAndMax()
        {
            var band = new RunTierBandAuthoring(2, 5).ToCore();
            Assert.AreEqual(2, band.MinTier);
            Assert.AreEqual(5, band.MaxTier);
        }

        [Test]
        public void Authoring_Default_IsAnOpenBand()
        {
            // A freshly constructed authoring value (an unset SerializeField) maps to the open default.
            var band = new RunTierBandAuthoring().ToCore();
            Assert.IsTrue(band.Contains(1));
            Assert.IsTrue(band.Contains(50));
        }
    }
}
