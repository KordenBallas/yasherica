using System;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Covers the Track F fullness read: the count-driven fill curve, the
    /// waterline-above-the-topmost-bubble guarantee, and the min/max clamps.
    /// </summary>
    [TestFixture]
    public class LiquidFillCalculatorTests
    {
        private const float MinHeight = 0.16f;
        private const float MaxHeight = 0.4f;
        private const int CountAtMax = 12;
        private const float Headroom = 0.05f;

        private LiquidFillSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new LiquidFillSettings(MinHeight, MaxHeight, CountAtMax, Headroom);
        }

        [Test]
        public void Settings_InvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LiquidFillSettings(0f, MaxHeight, CountAtMax, Headroom));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LiquidFillSettings(MinHeight, MinHeight - 0.01f, CountAtMax, Headroom));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LiquidFillSettings(MinHeight, MaxHeight, 0, Headroom));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LiquidFillSettings(MinHeight, MaxHeight, CountAtMax, -0.01f));
        }

        [Test]
        public void Calculate_NegativeCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LiquidFillCalculator.Calculate(-1, 0f, _settings));
        }

        [Test]
        public void Calculate_EmptyPot_SitsAtTheMinimum()
        {
            Assert.AreEqual(MinHeight, LiquidFillCalculator.Calculate(0, 0f, _settings));
        }

        [Test]
        public void Calculate_LevelRisesMonotonicallyWithCount()
        {
            float previous = 0f;
            for (int count = 1; count <= CountAtMax; count++)
            {
                float height = LiquidFillCalculator.Calculate(count, 0f, _settings);
                Assert.GreaterOrEqual(height, previous);
                previous = height;
            }
        }

        [Test]
        public void Calculate_FullPot_ReachesTheMaximumAndClampsBeyond()
        {
            Assert.AreEqual(MaxHeight, LiquidFillCalculator.Calculate(CountAtMax, 0f, _settings), 1e-6f);
            Assert.AreEqual(MaxHeight, LiquidFillCalculator.Calculate(CountAtMax * 3, 0f, _settings), 1e-6f);
        }

        [Test]
        public void Calculate_WaterlineAlwaysCoversTheTopmostBubble()
        {
            // Two items would sit low by count, but the bubble stack tops at 0.3.
            float height = LiquidFillCalculator.Calculate(2, 0.3f, _settings);

            Assert.GreaterOrEqual(height, 0.3f + Headroom - 1e-6f);
            Assert.LessOrEqual(height, MaxHeight);
        }

        [Test]
        public void Calculate_MaxClampWinsOverBubbleCoverage_TheOverfillEdge()
        {
            float height = LiquidFillCalculator.Calculate(20, MaxHeight + 0.2f, _settings);

            Assert.AreEqual(MaxHeight, height, 1e-6f);
        }
    }
}
