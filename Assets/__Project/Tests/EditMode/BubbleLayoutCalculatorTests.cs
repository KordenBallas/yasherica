using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BubbleLayoutCalculatorTests
    {
        private const float PotHalfWidth = 1.0f;
        private const float PotHalfHeight = 0.6f;
        private const float PotHalfDepth = 0.3f;

        private BubbleLayoutCalculator _calculator;
        private BubbleLayoutSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _calculator = new BubbleLayoutCalculator();
            _settings = new BubbleLayoutSettings(
                minBubbleRadius: 0.05f,
                maxBubbleRadius: 0.2f,
                radiusFalloff: 0.5f,
                edgePadding: 0.02f);
        }

        [Test]
        public void Calculate_ReturnsOnePlacementPerItem()
        {
            Assert.AreEqual(0, Calculate(0).Length);
            Assert.AreEqual(1, Calculate(1).Length);
            Assert.AreEqual(12, Calculate(12).Length);
        }

        [Test]
        public void Calculate_AllPlacementsStayInsidePotEllipse()
        {
            var placements = Calculate(25);

            foreach (var placement in placements)
            {
                float reachX = PotHalfWidth - placement.Radius - _settings.EdgePadding;
                float reachY = PotHalfHeight - placement.Radius - _settings.EdgePadding;
                float normalized =
                    (placement.X * placement.X) / (reachX * reachX) +
                    (placement.Y * placement.Y) / (reachY * reachY);

                Assert.LessOrEqual(normalized, 1.0001f,
                    $"Placement ({placement.X}, {placement.Y}) escapes the padded pot interior.");
            }
        }

        [Test]
        public void Calculate_DepthStaysInsidePaddedHalfDepth()
        {
            var placements = Calculate(25);

            foreach (var placement in placements)
            {
                float reachZ = PotHalfDepth - placement.Radius - _settings.EdgePadding;
                Assert.LessOrEqual(System.Math.Abs(placement.Z), reachZ + 0.0001f,
                    $"Placement depth {placement.Z} escapes the padded pot interior.");
            }
        }

        [Test]
        public void Calculate_DepthVariesAcrossBubbles()
        {
            var placements = Calculate(8);

            bool anyDifferent = false;
            for (int i = 1; i < placements.Length; i++)
            {
                if (placements[i].Z != placements[0].Z)
                {
                    anyDifferent = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifferent, "All bubbles share one depth plane.");
        }

        [Test]
        public void Calculate_ZeroHalfDepth_AllPlacementsAtZeroDepth()
        {
            var placements = _calculator.Calculate(10, PotHalfWidth, PotHalfHeight, 0f, _settings);

            foreach (var placement in placements)
            {
                Assert.AreEqual(0f, placement.Z);
            }
        }

        [Test]
        public void Calculate_BubblesNeverOverlapInScreenPlane()
        {
            // A tiny floor keeps the MinBubbleRadius clamp out of the way: the
            // no-overlap guarantee holds whenever the radius is free to shrink.
            var settings = new BubbleLayoutSettings(
                minBubbleRadius: 0.001f,
                maxBubbleRadius: _settings.MaxBubbleRadius,
                radiusFalloff: _settings.RadiusFalloff,
                edgePadding: _settings.EdgePadding);

            for (int count = 2; count <= 30; count++)
            {
                var placements = _calculator.Calculate(
                    count, PotHalfWidth, PotHalfHeight, PotHalfDepth, settings);

                for (int i = 0; i < placements.Length; i++)
                {
                    for (int j = i + 1; j < placements.Length; j++)
                    {
                        float dx = placements[i].X - placements[j].X;
                        float dy = placements[i].Y - placements[j].Y;
                        float distance = (float)System.Math.Sqrt(dx * dx + dy * dy);

                        Assert.GreaterOrEqual(distance, 2f * placements[i].Radius - 0.0001f,
                            $"Bubbles {i} and {j} overlap at count {count} (distance {distance}, radius {placements[i].Radius}).");
                    }
                }
            }
        }

        [Test]
        public void Calculate_SingleBubbleKeepsFormulaRadius()
        {
            var single = Calculate(1);

            Assert.AreEqual(
                _settings.MaxBubbleRadius / (1f + _settings.RadiusFalloff),
                single[0].Radius,
                0.0001f);
        }

        [Test]
        public void Calculate_RadiusShrinksAsCountGrows()
        {
            float previousRadius = float.MaxValue;

            foreach (int count in new[] { 1, 4, 9, 16, 30 })
            {
                var placements = Calculate(count);
                Assert.LessOrEqual(placements[0].Radius, previousRadius,
                    $"Radius must not grow when count increases to {count}.");
                previousRadius = placements[0].Radius;
            }
        }

        [Test]
        public void Calculate_RadiusIsClampedToConfiguredRange()
        {
            var single = Calculate(1);
            var crowd = Calculate(500);

            Assert.LessOrEqual(single[0].Radius, _settings.MaxBubbleRadius);
            Assert.GreaterOrEqual(crowd[0].Radius, _settings.MinBubbleRadius);
        }

        [Test]
        public void Calculate_IsDeterministicForSameInputs()
        {
            var first = Calculate(7);
            var second = Calculate(7);

            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i].X, second[i].X);
                Assert.AreEqual(first[i].Y, second[i].Y);
                Assert.AreEqual(first[i].Z, second[i].Z);
                Assert.AreEqual(first[i].Radius, second[i].Radius);
                Assert.AreEqual(first[i].BobPhase, second[i].BobPhase);
            }
        }

        [Test]
        public void Calculate_BobPhasesAreDesynchronized()
        {
            var placements = Calculate(5);

            for (int i = 1; i < placements.Length; i++)
            {
                Assert.AreNotEqual(placements[0].BobPhase, placements[i].BobPhase);
            }
        }

        [Test]
        public void Calculate_NegativeCount_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Calculate(-1));
        }

        private BubblePlacement[] Calculate(int count)
        {
            return _calculator.Calculate(count, PotHalfWidth, PotHalfHeight, PotHalfDepth, _settings);
        }
    }
}
