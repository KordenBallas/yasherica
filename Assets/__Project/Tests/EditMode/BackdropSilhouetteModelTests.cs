using LevelGeneration.Route;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BackdropSilhouetteModelTests
    {
        [Test]
        public void SameSeedAndLayer_ReplayHeightsExactly()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            var first = new BackdropSilhouetteModel(settings, seed: 42);
            var second = new BackdropSilhouetteModel(settings, seed: 42);

            CollectionAssert.AreEqual(
                first.GetRidgeHeights(0, 96, 900f),
                second.GetRidgeHeights(0, 96, 900f));
        }

        [Test]
        public void DifferentLayers_ProduceDifferentRidges()
        {
            var model = new BackdropSilhouetteModel(BiomeLandscapeSettings.CreateDefault(), seed: 7);

            CollectionAssert.AreNotEqual(
                model.GetRidgeHeights(0, 96, 900f),
                model.GetRidgeHeights(1, 96, 900f));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentRidges()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();

            CollectionAssert.AreNotEqual(
                new BackdropSilhouetteModel(settings, seed: 1).GetRidgeHeights(0, 96, 900f),
                new BackdropSilhouetteModel(settings, seed: 2).GetRidgeHeights(0, 96, 900f));
        }

        [Test]
        public void Heights_StayWithinTheRidgeAmplitude()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            var model = new BackdropSilhouetteModel(settings, seed: 11);

            foreach (float height in model.GetRidgeHeights(0, 256, 1200f))
            {
                Assert.GreaterOrEqual(height, 0f);
                Assert.LessOrEqual(height, settings.BackdropRidgeAmplitude);
            }
        }

        [Test]
        public void TinySampleCount_IsCoercedToAValidStrip()
        {
            var model = new BackdropSilhouetteModel(BiomeLandscapeSettings.CreateDefault(), seed: 3);
            Assert.AreEqual(2, model.GetRidgeHeights(0, sampleCount: 1, width: 100f).Length);
        }
    }
}
