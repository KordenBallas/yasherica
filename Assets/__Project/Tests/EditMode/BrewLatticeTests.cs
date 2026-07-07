using System;
using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Covers the Track F stable-spot lattice: bottom-up ordering, bowl
    /// containment, screen-plane separation, rim overflow, and determinism.
    /// </summary>
    [TestFixture]
    public class BrewLatticeTests
    {
        private const float BubbleRadius = 0.075f;
        private const float SpacingMargin = 1.15f;
        private const float EdgePadding = 0.02f;
        private const float FloorClearance = 0.01f;
        private const float DepthJitter = 0.05f;
        private const int OverflowLayers = 2;

        private CauldronProfileSettings _bowl;
        private BrewLatticeSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _bowl = new CauldronProfileSettings(0.5f, 0.42f, 0.05f, 0.06f, 6);
            _settings = new BrewLatticeSettings(
                BubbleRadius, SpacingMargin, EdgePadding, FloorClearance, DepthJitter, OverflowLayers);
        }

        [Test]
        public void Settings_InvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(0f, SpacingMargin, EdgePadding, FloorClearance, DepthJitter, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(BubbleRadius, 0.9f, EdgePadding, FloorClearance, DepthJitter, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(BubbleRadius, SpacingMargin, -0.01f, FloorClearance, DepthJitter, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(BubbleRadius, SpacingMargin, EdgePadding, -0.01f, DepthJitter, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(BubbleRadius, SpacingMargin, EdgePadding, FloorClearance, -0.01f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BrewLatticeSettings(BubbleRadius, SpacingMargin, EdgePadding, FloorClearance, DepthJitter, -1));
        }

        [Test]
        public void Build_YieldsSpotsAndSequentialIndices()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            Assert.Greater(spots.Count, 4, "The default bowl must hold a usable number of spots.");
            for (int i = 0; i < spots.Count; i++)
            {
                Assert.AreEqual(i, spots[i].Index);
            }
        }

        [Test]
        public void Build_SpotOrderIsBottomUp()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            for (int i = 1; i < spots.Count; i++)
            {
                Assert.GreaterOrEqual(spots[i].Y, spots[i - 1].Y - 1e-5f,
                    "Later spots must never sit below earlier ones (bottom-up fill).");
            }
        }

        [Test]
        public void Build_SpotsStayAboveTheFloorAndInsideTheWall()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            foreach (var spot in spots)
            {
                Assert.GreaterOrEqual(spot.Y - BubbleRadius, _bowl.WallThickness - 1e-5f,
                    "No bubble may sink into the bowl floor.");

                float sampleHeight = Math.Min(spot.Y, _bowl.BowlDepth);
                float innerRadius = CauldronProfileCalculator.InnerRadiusAtHeight(_bowl, sampleHeight);
                Assert.LessOrEqual(Math.Abs(spot.X) + BubbleRadius, innerRadius + 1e-5f,
                    "No bubble may poke through the bowl wall.");
            }
        }

        [Test]
        public void Build_SubmergedRowsStayBelowTheRim_OverflowRowsAreLimited()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            var rowHeights = new SortedSet<float>();
            foreach (var spot in spots)
            {
                rowHeights.Add((float)Math.Round(spot.Y, 5));
            }

            int overflowRows = 0;
            foreach (float y in rowHeights)
            {
                if (y + BubbleRadius > _bowl.BowlDepth)
                {
                    overflowRows++;
                }
            }

            Assert.LessOrEqual(overflowRows, OverflowLayers);
        }

        [Test]
        public void Build_SpotsDoNotOverlapOnTheScreenPlane()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            // The stage camera views the bowl from the front, so X/Y is the
            // screen plane the non-overlap invariant must hold on.
            for (int i = 0; i < spots.Count; i++)
            {
                for (int j = i + 1; j < spots.Count; j++)
                {
                    float dx = spots[i].X - spots[j].X;
                    float dy = spots[i].Y - spots[j].Y;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    Assert.GreaterOrEqual(distance, BubbleRadius * 2f - 1e-5f,
                        $"Spots {i} and {j} overlap on screen.");
                }
            }
        }

        [Test]
        public void Build_SpotsAlignIntoVerticalColumns()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            // FR4's gravity settle drops a column straight down, so spots
            // sharing a column key must share one X, and rows one Y.
            var columnX = new Dictionary<int, float>();
            var rowY = new Dictionary<int, float>();
            foreach (var spot in spots)
            {
                if (columnX.TryGetValue(spot.Column, out float x))
                {
                    Assert.AreEqual(x, spot.X, 1e-6f, $"Column {spot.Column} must be vertical.");
                }
                else
                {
                    columnX[spot.Column] = spot.X;
                }

                if (rowY.TryGetValue(spot.Row, out float y))
                {
                    Assert.AreEqual(y, spot.Y, 1e-6f, $"Row {spot.Row} must be horizontal.");
                }
                else
                {
                    rowY[spot.Row] = spot.Y;
                }
            }

            Assert.Greater(columnX.Count, 1, "The bowl must be wide enough for several columns.");
        }

        [Test]
        public void Build_EachColumnsRowsAreContiguous()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            // A hole inside a column would let the settle "fall through" a
            // nonexistent spot; the bowl's unimodal width guarantees none.
            var rowsByColumn = new Dictionary<int, List<int>>();
            foreach (var spot in spots)
            {
                if (!rowsByColumn.TryGetValue(spot.Column, out var rows))
                {
                    rows = new List<int>();
                    rowsByColumn[spot.Column] = rows;
                }

                rows.Add(spot.Row);
            }

            foreach (var pair in rowsByColumn)
            {
                pair.Value.Sort();
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    Assert.AreEqual(pair.Value[i - 1] + 1, pair.Value[i],
                        $"Column {pair.Key} must have contiguous rows.");
                }
            }
        }

        [Test]
        public void Build_DepthJitterStaysBounded()
        {
            var spots = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            foreach (var spot in spots)
            {
                Assert.LessOrEqual(Math.Abs(spot.Z), DepthJitter + 1e-5f);
            }
        }

        [Test]
        public void Build_IsDeterministicForSameInput()
        {
            var first = BrewSpotLatticeBuilder.Build(_bowl, _settings);
            var second = BrewSpotLatticeBuilder.Build(_bowl, _settings);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].X, second[i].X);
                Assert.AreEqual(first[i].Y, second[i].Y);
                Assert.AreEqual(first[i].Z, second[i].Z);
            }
        }

        [Test]
        public void InnerRadiusAtHeight_WidensFromFloorToBellyAndStaysPositive()
        {
            float floorRadius = CauldronProfileCalculator.InnerRadiusAtHeight(_bowl, 0f);
            float bellyRadius = CauldronProfileCalculator.InnerRadiusAtHeight(_bowl, _bowl.BowlDepth * 0.6f);
            float overshootRadius = CauldronProfileCalculator.InnerRadiusAtHeight(_bowl, _bowl.BowlDepth * 2f);

            Assert.Greater(floorRadius, 0f);
            Assert.Greater(bellyRadius, floorRadius, "The belly must be wider than the base.");
            Assert.Greater(overshootRadius, 0f, "Rim overshoot must clamp, not collapse.");
            Assert.AreEqual(
                CauldronProfileCalculator.InnerRadiusAtHeight(_bowl, _bowl.BowlDepth),
                overshootRadius,
                1e-6f,
                "Heights above the rim must sample the mouth radius.");
        }
    }
}
