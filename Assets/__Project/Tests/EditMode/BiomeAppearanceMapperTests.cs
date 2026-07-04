using LevelGeneration.Route;
using NUnit.Framework;
using UnityEngine;
using World.Biomes.Data;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeAppearanceMapperTests
    {
        [Test]
        public void NullDefinition_FallsBackToDefaults()
        {
            var settings = BiomeAppearanceMapper.ToLandscapeSettings(null);

            Assert.AreEqual(BiomeLandscapeSettings.DefaultCorridorHalfWidth, settings.CorridorHalfWidth);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineWavelength, settings.BaselineWavelength);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineAmplitude, settings.BaselineAmplitude);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultArcSlotLength, settings.ArcSlotLength);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultArcChancePercent, settings.ArcChancePercent);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultArcDepth, settings.ArcDepth);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierCount, settings.TierCount);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierStep, settings.TierStep);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierWavelength, settings.TierWavelength);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBackdropRidgeAmplitude, settings.BackdropRidgeAmplitude);
        }

        [Test]
        public void FreshAsset_MapsItsSerializedDefaults()
        {
            var definition = ScriptableObject.CreateInstance<BiomeAppearanceDefinition>();
            try
            {
                var settings = BiomeAppearanceMapper.ToLandscapeSettings(definition);

                // The SO's inspector defaults must agree with the Core defaults, so an unedited
                // asset and a missing asset behave identically.
                Assert.AreEqual(BiomeLandscapeSettings.DefaultCorridorHalfWidth, settings.CorridorHalfWidth);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineWavelength, settings.BaselineWavelength);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineAmplitude, settings.BaselineAmplitude);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultArcSlotLength, settings.ArcSlotLength);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultArcChancePercent, settings.ArcChancePercent);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultArcDepth, settings.ArcDepth);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultArcHalfLengthMin, settings.ArcHalfLengthMin);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultArcHalfLengthMax, settings.ArcHalfLengthMax);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultLandmarkOffset, settings.LandmarkOffset);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultLandmarkScaleMin, settings.LandmarkScaleMin);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultLandmarkScaleMax, settings.LandmarkScaleMax);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultTierCount, settings.TierCount);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultTierStep, settings.TierStep);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultTierWavelength, settings.TierWavelength);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultBackdropRidgeAmplitude, settings.BackdropRidgeAmplitude);
                Assert.AreEqual(BiomeLandscapeSettings.DefaultBackdropRidgeWavelength, settings.BackdropRidgeWavelength);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
