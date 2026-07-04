using LevelGeneration.Route;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeLandscapeSettingsTests
    {
        private static BiomeLandscapeSettings Create(
            float corridorHalfWidth = BiomeLandscapeSettings.DefaultCorridorHalfWidth,
            float baselineWavelength = BiomeLandscapeSettings.DefaultBaselineWavelength,
            float baselineAmplitude = BiomeLandscapeSettings.DefaultBaselineAmplitude,
            float arcSlotLength = BiomeLandscapeSettings.DefaultArcSlotLength,
            int arcChancePercent = BiomeLandscapeSettings.DefaultArcChancePercent,
            float arcDepth = BiomeLandscapeSettings.DefaultArcDepth,
            float arcHalfLengthMin = BiomeLandscapeSettings.DefaultArcHalfLengthMin,
            float arcHalfLengthMax = BiomeLandscapeSettings.DefaultArcHalfLengthMax,
            float landmarkOffset = BiomeLandscapeSettings.DefaultLandmarkOffset,
            float landmarkScaleMin = BiomeLandscapeSettings.DefaultLandmarkScaleMin,
            float landmarkScaleMax = BiomeLandscapeSettings.DefaultLandmarkScaleMax,
            int tierCount = BiomeLandscapeSettings.DefaultTierCount,
            float tierStep = BiomeLandscapeSettings.DefaultTierStep,
            float tierWavelength = BiomeLandscapeSettings.DefaultTierWavelength,
            float backdropRidgeAmplitude = BiomeLandscapeSettings.DefaultBackdropRidgeAmplitude,
            float backdropRidgeWavelength = BiomeLandscapeSettings.DefaultBackdropRidgeWavelength)
        {
            return new BiomeLandscapeSettings(
                corridorHalfWidth, baselineWavelength, baselineAmplitude,
                arcSlotLength, arcChancePercent, arcDepth, arcHalfLengthMin, arcHalfLengthMax,
                landmarkOffset, landmarkScaleMin, landmarkScaleMax,
                tierCount, tierStep, tierWavelength,
                backdropRidgeAmplitude, backdropRidgeWavelength);
        }

        [Test]
        public void NegativesAndZeros_ClampToSafeValues()
        {
            var settings = new BiomeLandscapeSettings(
                0f, -1f, -1f, 0f, -5, -1f, -1f, -1f, -1f, -1f, -1f, -3, -1f, 0f, -1f, 0f);

            Assert.AreEqual(BiomeLandscapeSettings.DefaultCorridorHalfWidth, settings.CorridorHalfWidth);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineWavelength, settings.BaselineWavelength);
            Assert.AreEqual(0f, settings.BaselineAmplitude);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultArcSlotLength, settings.ArcSlotLength);
            Assert.AreEqual(0, settings.ArcChancePercent);
            Assert.AreEqual(0f, settings.ArcDepth);
            Assert.AreEqual(1f, settings.ArcHalfLengthMin);
            Assert.AreEqual(1f, settings.ArcHalfLengthMax);
            Assert.AreEqual(BiomeLandscapeSettings.MinLandmarkClearance, settings.LandmarkOffset);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultLandmarkScaleMin, settings.LandmarkScaleMin);
            Assert.AreEqual(settings.LandmarkScaleMin, settings.LandmarkScaleMax);
            Assert.AreEqual(1, settings.TierCount);
            Assert.AreEqual(0f, settings.TierStep);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierWavelength, settings.TierWavelength);
            Assert.AreEqual(0f, settings.BackdropRidgeAmplitude);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBackdropRidgeWavelength, settings.BackdropRidgeWavelength);
        }

        [Test]
        public void BaselineAmplitude_IsCappedSoArcsStillFitTheCorridor()
        {
            var settings = Create(corridorHalfWidth: 8f, arcDepth: 3f, baselineAmplitude: 100f);
            Assert.AreEqual(5f, settings.BaselineAmplitude);
        }

        [Test]
        public void ArcDepth_IsCappedToTheCorridor()
        {
            var settings = Create(corridorHalfWidth: 8f, arcDepth: 20f);
            Assert.AreEqual(8f, settings.ArcDepth);
            Assert.AreEqual(0f, settings.BaselineAmplitude);
        }

        [Test]
        public void ArcHalfLengthMax_IsCappedToItsSlotFraction()
        {
            var settings = Create(arcSlotLength: 100f, arcHalfLengthMax: 90f, arcHalfLengthMin: 10f);
            Assert.AreEqual(100f * BiomeLandscapeSettings.MaxArcHalfLengthSlotFraction, settings.ArcHalfLengthMax);
            Assert.AreEqual(10f, settings.ArcHalfLengthMin);
        }

        [Test]
        public void ArcChance_ClampsToPercentRange()
        {
            Assert.AreEqual(100, Create(arcChancePercent: 250).ArcChancePercent);
        }

        [Test]
        public void LandmarkOffset_IsRaisedToClearTheLocalBaselineSwing()
        {
            var settings = Create(baselineAmplitude: 5f, landmarkOffset: 1f);
            Assert.AreEqual(2f * 5f + BiomeLandscapeSettings.MinLandmarkClearance, settings.LandmarkOffset);
        }

        [Test]
        public void CreateDefault_MatchesTheNamedDefaults()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            Assert.AreEqual(BiomeLandscapeSettings.DefaultCorridorHalfWidth, settings.CorridorHalfWidth);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBaselineAmplitude, settings.BaselineAmplitude);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultArcChancePercent, settings.ArcChancePercent);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierCount, settings.TierCount);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultTierStep, settings.TierStep);
            Assert.AreEqual(BiomeLandscapeSettings.DefaultBackdropRidgeAmplitude, settings.BackdropRidgeAmplitude);
        }
    }
}
