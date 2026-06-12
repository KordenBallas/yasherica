using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BubbleDriftCalculatorTests
    {
        private BubbleDriftSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new BubbleDriftSettings(amplitude: 0.01f, frequency: 0.35f);
        }

        [Test]
        public void SampleOffset_ZeroAmplitude_IsStill()
        {
            var still = new BubbleDriftSettings(0f, 0.35f);

            BubbleDriftCalculator.SampleOffset(1.7f, 0.4f, still, out float x, out float y);

            Assert.AreEqual(0f, x);
            Assert.AreEqual(0f, y);
        }

        [Test]
        public void SampleOffset_StaysWithinAmplitudePerAxis()
        {
            for (int pi = 0; pi <= 5; pi++)
            {
                for (int ti = 0; ti <= 20; ti++)
                {
                    BubbleDriftCalculator.SampleOffset(
                        ti * 0.31f, pi * 1.13f, _settings, out float x, out float y);
                    Assert.LessOrEqual(System.Math.Abs(x), _settings.Amplitude + 1e-5f);
                    Assert.LessOrEqual(System.Math.Abs(y), _settings.Amplitude + 1e-5f);
                }
            }
        }

        [Test]
        public void SampleOffset_IsDeterministicForSameInputs()
        {
            BubbleDriftCalculator.SampleOffset(2.5f, 0.8f, _settings, out float firstX, out float firstY);
            BubbleDriftCalculator.SampleOffset(2.5f, 0.8f, _settings, out float secondX, out float secondY);

            Assert.AreEqual(firstX, secondX);
            Assert.AreEqual(firstY, secondY);
        }

        [Test]
        public void SampleOffset_VariesOverTime()
        {
            BubbleDriftCalculator.SampleOffset(0f, 0.4f, _settings, out float aX, out float aY);
            BubbleDriftCalculator.SampleOffset(0.2f, 0.4f, _settings, out float bX, out float bY);

            Assert.IsTrue(aX != bX || aY != bY, "The drift must animate as time advances.");
        }

        [Test]
        public void SampleOffset_DifferentPhases_Desync()
        {
            BubbleDriftCalculator.SampleOffset(1f, 0f, _settings, out float aX, out float aY);
            BubbleDriftCalculator.SampleOffset(1f, 2.4f, _settings, out float bX, out float bY);

            Assert.AreNotEqual(aX, bX, "Bubbles with different phases must not move in lockstep.");
            Assert.AreNotEqual(aY, bY, "Bubbles with different phases must not move in lockstep.");
        }

        [Test]
        public void SampleOffset_MotionIsTwoDimensional()
        {
            // Three samples whose (x, y) points are collinear through the origin would
            // mean the path degenerates into a straight line instead of a wander.
            BubbleDriftCalculator.SampleOffset(0.3f, 0.5f, _settings, out float aX, out float aY);
            BubbleDriftCalculator.SampleOffset(0.9f, 0.5f, _settings, out float bX, out float bY);
            BubbleDriftCalculator.SampleOffset(1.6f, 0.5f, _settings, out float cX, out float cY);

            float crossAb = aX * bY - aY * bX;
            float crossAc = aX * cY - aY * cX;

            Assert.IsTrue(
                System.Math.Abs(crossAb) > 1e-7f || System.Math.Abs(crossAc) > 1e-7f,
                "The drift path must be quasi-circular, not a straight line.");
        }
    }
}
