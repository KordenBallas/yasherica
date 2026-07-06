using System.Collections.Generic;
using NUnit.Framework;
using World.Dressing.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class BackdropScatterPlannerTests
    {
        private const int RunSeed = 777;
        private const float Density = 4f;

        private static IReadOnlyList<BackdropEntryData> Entries()
        {
            return new[]
            {
                new BackdropEntryData(3, 3f, 6f),
                new BackdropEntryData(2, 4f, 8f),
                new BackdropEntryData(1, 2f, 5f)
            };
        }

        private static IReadOnlyList<BackdropScatterPlacement> Plan(
            float fromX, float toX, IReadOnlyList<BackdropEntryData> entries = null,
            float density = Density, int seed = RunSeed)
        {
            return new BackdropScatterPlanner().PlanSpan(fromX, toX, entries ?? Entries(), density, seed);
        }

        [Test]
        public void SameSpan_ProducesIdenticalPlacements()
        {
            var first = Plan(0f, 300f);
            var second = Plan(0f, 300f);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].EntryIndex, second[i].EntryIndex);
                Assert.AreEqual(first[i].X, second[i].X);
                Assert.AreEqual(first[i].Z, second[i].Z);
                Assert.AreEqual(first[i].YawDegrees, second[i].YawDegrees);
                Assert.AreEqual(first[i].Scale, second[i].Scale);
            }
        }

        [Test]
        public void ContiguousSpans_EqualTheSingleSpan()
        {
            // The streaming windows' half-open spans must fill every slot exactly once, and a
            // restored run (replaying different span boundaries is impossible — but window sizes
            // vary) yields the same horizon as one big span.
            var whole = Plan(0f, 250f);
            var pieces = new List<BackdropScatterPlacement>();
            pieces.AddRange(Plan(0f, 60f));
            pieces.AddRange(Plan(60f, 140f));
            pieces.AddRange(Plan(140f, 250f));

            Assert.AreEqual(whole.Count, pieces.Count);
            for (int i = 0; i < whole.Count; i++)
            {
                Assert.AreEqual(whole[i].X, pieces[i].X, 1e-5f);
                Assert.AreEqual(whole[i].Z, pieces[i].Z, 1e-5f);
                Assert.AreEqual(whole[i].EntryIndex, pieces[i].EntryIndex);
            }
        }

        [Test]
        public void Placements_StayInsideTheDepthBandAndTheirSlots()
        {
            var placements = Plan(0f, 500f);
            Assert.Greater(placements.Count, 0);
            foreach (var placement in placements)
            {
                Assert.GreaterOrEqual(placement.Z, BackdropScatterPlanner.BandNearZ);
                Assert.LessOrEqual(placement.Z, BackdropScatterPlanner.BandFarZ);
                Assert.GreaterOrEqual(placement.X, 0f);
                Assert.Less(placement.X, 500f + BackdropScatterPlanner.SlotLength);
            }
        }

        [Test]
        public void Density_ScalesTheCount_AndZeroPlacesNothing()
        {
            int sparse = Plan(0f, 1000f, density: 2f).Count;
            int dense = Plan(0f, 1000f, density: 8f).Count;
            Assert.Greater(dense, sparse);
            Assert.AreEqual(0, Plan(0f, 1000f, density: 0f).Count);
        }

        [Test]
        public void EmptyOrWeightlessEntries_PlaceNothing()
        {
            Assert.AreEqual(0, Plan(0f, 500f, new BackdropEntryData[0]).Count);
            Assert.AreEqual(0, Plan(0f, 500f, new[] { new BackdropEntryData(0, 1f, 2f) }).Count);
        }

        [Test]
        public void WeightZeroEntry_IsNeverDrawn_AndScalesStayInRange()
        {
            var entries = new[]
            {
                new BackdropEntryData(0, 9f, 9f),
                new BackdropEntryData(1, 3f, 6f)
            };
            var placements = Plan(0f, 800f, entries);
            Assert.Greater(placements.Count, 0);
            foreach (var placement in placements)
            {
                Assert.AreEqual(1, placement.EntryIndex, "Weight-0 entry must never be drawn.");
                Assert.GreaterOrEqual(placement.Scale, 3f);
                Assert.LessOrEqual(placement.Scale, 6f);
            }
        }

        [Test]
        public void LowDensity_StaysAHorizonNotACrowd()
        {
            // The budget requirement (brief FR6): the default dial reads sparse — on the order of
            // a few silhouettes per 100 units, never a dense field.
            var placements = Plan(0f, 1000f, density: 4f);
            Assert.LessOrEqual(placements.Count, 60, "Default density should stay sparse.");
            Assert.GreaterOrEqual(placements.Count, 20, "But not empty — a horizon needs shapes.");
        }
    }
}
