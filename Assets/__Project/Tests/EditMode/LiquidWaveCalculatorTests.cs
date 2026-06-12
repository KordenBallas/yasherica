using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LiquidWaveCalculatorTests
    {
        private LiquidWaveSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new LiquidWaveSettings(
                amplitude: 0.05f,
                frequency: 1.2f,
                spatialScale: 6f,
                secondaryWeight: 0.5f);
        }

        [Test]
        public void SampleHeight_ZeroAmplitude_IsFlat()
        {
            var flat = new LiquidWaveSettings(0f, 1.2f, 6f, 0.5f);

            Assert.AreEqual(0f, LiquidWaveCalculator.SampleHeight(0.3f, -0.2f, 1.7f, flat));
            Assert.AreEqual(0f, LiquidWaveCalculator.SampleHeight(-1f, 2f, 0f, flat));
        }

        [Test]
        public void SampleHeight_StaysWithinAmplitude()
        {
            for (int xi = -5; xi <= 5; xi++)
            {
                for (int ti = 0; ti <= 20; ti++)
                {
                    float height = LiquidWaveCalculator.SampleHeight(
                        xi * 0.13f, xi * -0.07f, ti * 0.31f, _settings);
                    Assert.LessOrEqual(System.Math.Abs(height), _settings.Amplitude + 1e-5f);
                }
            }
        }

        [Test]
        public void SampleHeight_IsDeterministicForSameInputs()
        {
            float first = LiquidWaveCalculator.SampleHeight(0.21f, -0.34f, 2.5f, _settings);
            float second = LiquidWaveCalculator.SampleHeight(0.21f, -0.34f, 2.5f, _settings);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void SampleHeight_VariesAcrossTheSurface()
        {
            float a = LiquidWaveCalculator.SampleHeight(0f, 0f, 1f, _settings);
            float b = LiquidWaveCalculator.SampleHeight(0.25f, 0.1f, 1f, _settings);

            Assert.AreNotEqual(a, b, "Two distinct surface points must not share the same height.");
        }

        [Test]
        public void SampleHeight_VariesOverTime()
        {
            float a = LiquidWaveCalculator.SampleHeight(0.1f, 0.1f, 0f, _settings);
            float b = LiquidWaveCalculator.SampleHeight(0.1f, 0.1f, 0.2f, _settings);

            Assert.AreNotEqual(a, b, "The surface must animate as time advances.");
        }

        [Test]
        public void SampleHeight_NegativeSecondaryWeight_IsIgnored()
        {
            var noSecondary = new LiquidWaveSettings(0.05f, 1.2f, 6f, 0f);
            var negative = new LiquidWaveSettings(0.05f, 1.2f, 6f, -2f);

            Assert.AreEqual(
                LiquidWaveCalculator.SampleHeight(0.4f, 0.2f, 1.1f, noSecondary),
                LiquidWaveCalculator.SampleHeight(0.4f, 0.2f, 1.1f, negative));
        }
    }
}
