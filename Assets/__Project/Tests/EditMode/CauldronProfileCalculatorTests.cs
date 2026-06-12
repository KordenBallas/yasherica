using System;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CauldronProfileCalculatorTests
    {
        private const float BowlRadius = 0.5f;
        private const float BowlDepth = 0.4f;
        private const float WallThickness = 0.04f;
        private const float RimWidth = 0.05f;
        private const int WallSegments = 6;

        private CauldronProfileSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new CauldronProfileSettings(
                BowlRadius, BowlDepth, WallThickness, RimWidth, WallSegments);
        }

        [Test]
        public void Calculate_OuterAndInnerArePairedWithExpectedCount()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            // Axis point + belly curve + rim lip.
            Assert.AreEqual(WallSegments + 2, profile.Outer.Count);
            Assert.AreEqual(profile.Outer.Count, profile.Inner.Count);
        }

        [Test]
        public void Calculate_InnerWallIsInsetByWallThicknessAlongTheBelly()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            for (int i = 1; i < profile.Outer.Count - 1; i++)
            {
                Assert.AreEqual(WallThickness, profile.Outer[i].Radius - profile.Inner[i].Radius, 1e-5f,
                    $"Belly pair {i} must be inset by the wall thickness.");
            }
        }

        [Test]
        public void Calculate_RimIsTheHighestPointAtBowlDepth()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            var rim = profile.Outer[profile.Outer.Count - 1];
            Assert.AreEqual(BowlDepth, rim.Height, 1e-5f);

            foreach (var point in profile.Outer)
            {
                Assert.LessOrEqual(point.Height, BowlDepth + 1e-5f);
            }
        }

        [Test]
        public void Calculate_RimLipFlaresOutwardByRimWidth()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            int last = profile.Outer.Count - 1;
            Assert.AreEqual(RimWidth, profile.Outer[last].Radius - profile.Outer[last - 1].Radius, 1e-5f);
        }

        [Test]
        public void Calculate_BellyIsWiderThanBaseAndMouth()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            float widest = 0f;
            for (int i = 1; i < profile.Outer.Count - 1; i++)
            {
                widest = Math.Max(widest, profile.Outer[i].Radius);
            }

            Assert.Greater(widest, profile.Outer[1].Radius, "Belly must be wider than the base.");
            Assert.Greater(widest, profile.Outer[profile.Outer.Count - 2].Radius,
                "Belly must be wider than the mouth so the pot reads as a cauldron.");
            Assert.AreEqual(BowlRadius, widest, BowlRadius * 0.05f,
                "Widest point must approximate the configured bowl radius.");
        }

        [Test]
        public void Calculate_AllPointsHavePositiveRadiusAndNonNegativeHeight()
        {
            var profile = CauldronProfileCalculator.Calculate(_settings);

            for (int i = 0; i < profile.Outer.Count; i++)
            {
                Assert.Greater(profile.Outer[i].Radius, 0f);
                Assert.Greater(profile.Inner[i].Radius, 0f);
                Assert.GreaterOrEqual(profile.Outer[i].Height, 0f);
                Assert.GreaterOrEqual(profile.Inner[i].Height, 0f);
            }
        }

        [Test]
        public void Calculate_IsDeterministicForSameSettings()
        {
            var first = CauldronProfileCalculator.Calculate(_settings);
            var second = CauldronProfileCalculator.Calculate(_settings);

            for (int i = 0; i < first.Outer.Count; i++)
            {
                Assert.AreEqual(first.Outer[i].Radius, second.Outer[i].Radius);
                Assert.AreEqual(first.Outer[i].Height, second.Outer[i].Height);
                Assert.AreEqual(first.Inner[i].Radius, second.Inner[i].Radius);
                Assert.AreEqual(first.Inner[i].Height, second.Inner[i].Height);
            }
        }

        [Test]
        public void Settings_InvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(0f, BowlDepth, WallThickness, RimWidth, WallSegments));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(BowlRadius, 0f, WallThickness, RimWidth, WallSegments));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(BowlRadius, BowlDepth, 0f, RimWidth, WallSegments));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(BowlRadius, BowlDepth, BowlRadius, RimWidth, WallSegments));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(BowlRadius, BowlDepth, WallThickness, -0.01f, WallSegments));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CauldronProfileSettings(BowlRadius, BowlDepth, WallThickness, RimWidth, 2));
        }
    }
}
