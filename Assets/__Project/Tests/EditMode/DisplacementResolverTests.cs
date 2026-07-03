using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DisplacementResolverTests
    {
        private HexDirectionConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        [Test]
        public void Push_MovesFullDistance_WhenPathIsClear()
        {
            var destination = DisplacementResolver.ResolveDestination(
                new HexCoordinates(0, 0), HexDirection.E, 2, _config,
                isCellValid: _ => true,
                isCellOccupied: _ => false);

            Assert.AreEqual(new HexCoordinates(2, 0), destination);
        }

        [Test]
        public void Push_StopsBeforeInvalidCell()
        {
            // Only Q <= 1 is on the board.
            var destination = DisplacementResolver.ResolveDestination(
                new HexCoordinates(0, 0), HexDirection.E, 3, _config,
                isCellValid: cell => cell.Q <= 1,
                isCellOccupied: _ => false);

            Assert.AreEqual(new HexCoordinates(1, 0), destination);
        }

        [Test]
        public void Push_StopsBeforeOccupiedCell()
        {
            var blocker = new HexCoordinates(2, 0);
            var destination = DisplacementResolver.ResolveDestination(
                new HexCoordinates(0, 0), HexDirection.E, 3, _config,
                isCellValid: _ => true,
                isCellOccupied: cell => cell.Equals(blocker));

            Assert.AreEqual(new HexCoordinates(1, 0), destination);
        }

        [Test]
        public void Push_ZeroDistance_StaysInPlace()
        {
            var origin = new HexCoordinates(3, -1);
            var destination = DisplacementResolver.ResolveDestination(
                origin, HexDirection.W, 0, _config,
                isCellValid: _ => true,
                isCellOccupied: _ => false);

            Assert.AreEqual(origin, destination);
        }

        [Test]
        public void Push_BlockedImmediately_StaysInPlace()
        {
            var origin = new HexCoordinates(0, 0);
            var destination = DisplacementResolver.ResolveDestination(
                origin, HexDirection.E, 2, _config,
                isCellValid: _ => true,
                isCellOccupied: _ => true);

            Assert.AreEqual(origin, destination);
        }
    }
}
