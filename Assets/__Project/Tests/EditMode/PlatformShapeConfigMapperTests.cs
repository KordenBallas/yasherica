using Combat.Battlefield;
using LevelGeneration.Data;
using LevelGeneration.Surface;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformShapeConfigMapperTests
    {
        [Test]
        public void NullConfig_FallsBackToDefaults()
        {
            var settings = PlatformShapeConfigMapper.ToSettings(null);

            Assert.AreEqual(PlatformShapeSettings.DefaultHexSize, settings.HexSize);
            Assert.AreEqual(PlatformShapeSettings.DefaultOrientation, settings.Orientation);
            Assert.AreEqual(PlatformShapeSettings.DefaultBattlefieldMinimumCells, settings.BattlefieldMinimumCells);
            Assert.AreEqual(PlatformShapeSettings.DefaultRimWidth, settings.RimWidth);
            Assert.AreEqual(PlatformShapeSettings.DefaultCellInset, settings.CellInset);
            Assert.NotNull(settings.Combat);
        }

        [Test]
        public void FreshAsset_MapsItsSerializedDefaults()
        {
            var config = ScriptableObject.CreateInstance<PlatformShapeConfig>();
            try
            {
                var settings = PlatformShapeConfigMapper.ToSettings(config);

                // The SO's inspector defaults must agree with the Core defaults, so an unedited asset
                // and a missing asset behave identically.
                Assert.AreEqual(PlatformShapeSettings.DefaultHexSize, settings.HexSize);
                Assert.AreEqual(HexOrientation.Flat, settings.Orientation);
                Assert.AreEqual(12, settings.BattlefieldMinimumCells);
                Assert.AreEqual(12, settings.Combat.MinCells);
                Assert.AreEqual(18, settings.Combat.MaxCells);
                Assert.AreEqual(6, settings.Combat.Compactness);
                Assert.AreEqual(2, settings.Empty.MinCells);
                Assert.AreEqual(3, settings.Loot.MinCells);
                Assert.AreEqual(4, settings.Npc.MinCells);
                Assert.AreEqual(1.2f, settings.RimWidth);
                Assert.AreEqual(35, settings.RimJitterPercent);
                Assert.AreEqual(0.4f, settings.RimDropHeight);
                Assert.AreEqual(1f, settings.PlatformThickness);
                Assert.AreEqual(2f, settings.GapBetweenPlatforms);
                Assert.AreEqual(1.5f, settings.HeightDeviation);
                Assert.AreEqual(0.06f, settings.CellInset);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
