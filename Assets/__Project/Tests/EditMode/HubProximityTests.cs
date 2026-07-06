using Hub.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 Hub walk-up interaction core: of all F-spots whose circle contains the player, the
    /// nearest wins the prompt; out-of-range means no prompt at all.
    /// </summary>
    [TestFixture]
    public class HubProximityTests
    {
        private static HubInteractionSpot Spot(string id, float x, float z, float radius = 2f) =>
            new HubInteractionSpot(id, x, z, radius);

        [Test]
        public void NothingInRange_ReturnsMinusOne()
        {
            var spots = new[] { Spot("a", 10f, 0f), Spot("b", 0f, 10f) };

            Assert.AreEqual(-1, HubProximity.FindNearest(0f, 0f, spots));
        }

        [Test]
        public void NearestInRangeSpot_Wins()
        {
            var spots = new[] { Spot("far", 1.8f, 0f), Spot("near", 0.5f, 0f), Spot("out", 10f, 0f) };

            Assert.AreEqual(1, HubProximity.FindNearest(0f, 0f, spots));
        }

        [Test]
        public void RadiusBoundary_IsInclusive()
        {
            var spots = new[] { Spot("edge", 2f, 0f, radius: 2f) };

            Assert.AreEqual(0, HubProximity.FindNearest(0f, 0f, spots));
        }

        [Test]
        public void PerSpotRadii_AreRespected()
        {
            // The nearer spot has a tiny circle the player is outside of; the farther, wider
            // circle is the one actually containing him.
            var spots = new[] { Spot("tight", 1f, 0f, radius: 0.5f), Spot("wide", 3f, 0f, radius: 4f) };

            Assert.AreEqual(1, HubProximity.FindNearest(0f, 0f, spots));
        }

        [Test]
        public void PlanarDistanceOnly_YIsIgnoredByDesign()
        {
            // Spots carry XZ only — a raised label or sunken disc must not change the decision.
            var spots = new[] { Spot("a", 1f, 1f) };

            Assert.AreEqual(0, HubProximity.FindNearest(0f, 0f, spots));
        }

        [Test]
        public void NullsAreSkipped()
        {
            var spots = new HubInteractionSpot[] { null, Spot("a", 0.5f, 0f) };

            Assert.AreEqual(1, HubProximity.FindNearest(0f, 0f, spots));
            Assert.AreEqual(-1, HubProximity.FindNearest(0f, 0f, null));
        }
    }
}
