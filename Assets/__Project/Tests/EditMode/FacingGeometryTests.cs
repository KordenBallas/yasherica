using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class FacingGeometryTests
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
        public void OffsetFor_MatchesConfiguredAxialOffset_ForAllSixDirections()
        {
            foreach (var mapping in TestHexDirectionConfig.FlatTopOffsets)
            {
                var offset = FacingGeometry.OffsetFor(mapping.direction, _config);

                Assert.AreEqual(
                    new HexCoordinates(mapping.offset.x, mapping.offset.y),
                    offset,
                    $"offset for {mapping.direction}");
            }
        }

        [Test]
        public void Neighbor_StepsOneCellAlongFacing()
        {
            var from = new HexCoordinates(2, -1);

            var neighbor = FacingGeometry.Neighbor(from, HexDirection.SW, _config);

            Assert.AreEqual(new HexCoordinates(1, 0), neighbor);
        }
    }
}
