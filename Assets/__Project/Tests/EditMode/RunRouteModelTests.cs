using System;
using System.Collections.Generic;
using System.Linq;
using LevelGeneration.Route;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunRouteModelTests
    {
        private const float SweepLength = 3000f;
        private const float FloatTolerance = 1e-3f;

        private static RunRouteModel CreateModel(int seed = 7, BiomeLandscapeSettings settings = null)
        {
            return new RunRouteModel(settings ?? BiomeLandscapeSettings.CreateDefault(), seed);
        }

        [Test]
        public void SameSeed_ReplaysSamplesExactly()
        {
            var first = CreateModel(seed: 42);
            var second = CreateModel(seed: 42);

            for (float x = 0f; x <= SweepLength; x += 7f)
            {
                var a = first.Sample(x);
                var b = second.Sample(x);
                Assert.AreEqual(a.LateralZ, b.LateralZ, $"LateralZ diverged at x={x}");
                Assert.AreEqual(a.TierIndex, b.TierIndex, $"TierIndex diverged at x={x}");
                Assert.AreEqual(a.TierY, b.TierY, $"TierY diverged at x={x}");
            }

            CollectionAssert.AreEqual(
                first.GetLandmarksInRange(0f, SweepLength).Select(l => l.X).ToList(),
                second.GetLandmarksInRange(0f, SweepLength).Select(l => l.X).ToList());
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentRoutes()
        {
            var first = CreateModel(seed: 1);
            var second = CreateModel(seed: 2);

            bool anyDifferent = false;
            for (float x = 0f; x <= SweepLength && !anyDifferent; x += 11f)
            {
                anyDifferent = Math.Abs(first.Sample(x).LateralZ - second.Sample(x).LateralZ) > FloatTolerance;
            }

            Assert.IsTrue(anyDifferent, "Two different seeds produced an identical lateral route.");
        }

        [Test]
        public void Lateral_StaysWithinTheCorridor()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            for (int seed = 0; seed < 10; seed++)
            {
                var model = CreateModel(seed, settings);
                for (float x = 0f; x <= SweepLength; x += 1f)
                {
                    float z = model.Sample(x).LateralZ;
                    Assert.LessOrEqual(Math.Abs(z), settings.CorridorHalfWidth + FloatTolerance,
                        $"Corridor bound violated at seed={seed}, x={x}: z={z}");
                }
            }
        }

        [Test]
        public void Arcs_OnlyBulgeTowardTheCamera()
        {
            // Zero baseline amplitude isolates the arc contribution: any non-zero lateral value is
            // an arc, and it must carry the toward-camera sign (never a bulge away from the camera).
            var settings = new BiomeLandscapeSettings(
                corridorHalfWidth: 8f, baselineWavelength: 90f, baselineAmplitude: 0f,
                arcSlotLength: 140f, arcChancePercent: 100, arcDepth: 3f,
                arcHalfLengthMin: 25f, arcHalfLengthMax: 42f,
                landmarkOffset: 14f, landmarkScaleMin: 6f, landmarkScaleMax: 10f,
                tierCount: 4, tierStep: 1.75f, tierWavelength: 300f,
                backdropRidgeAmplitude: 8f, backdropRidgeWavelength: 60f);

            for (int seed = 0; seed < 5; seed++)
            {
                var model = CreateModel(seed, settings);
                for (float x = 0f; x <= SweepLength; x += 1f)
                {
                    float z = model.Sample(x).LateralZ;
                    // TowardCameraSign is -1, so a pure-arc lateral value must never be positive.
                    Assert.GreaterOrEqual(z * RunRouteModel.TowardCameraSign, -FloatTolerance,
                        $"Arc bulged away from the camera at seed={seed}, x={x}: z={z}");
                }
            }
        }

        [Test]
        public void Trail_HasInertia_LateralSlopeStaysUnderTheAnalyticBound()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();

            // Baseline: d/dx of two weighted sinusoids; arc: max derivative of the cos² bump.
            float twoPi = 2f * (float)Math.PI;
            float baselineSlope = settings.BaselineAmplitude
                * (0.7f * twoPi / settings.BaselineWavelength
                   + 0.3f * twoPi / (settings.BaselineWavelength * 2.6f));
            float arcSlope = settings.ArcDepth * (float)Math.PI / (2f * settings.ArcHalfLengthMin);
            float maxSlope = baselineSlope + arcSlope;

            const float step = 1f;
            for (int seed = 0; seed < 5; seed++)
            {
                var model = CreateModel(seed, settings);
                float previous = model.Sample(0f).LateralZ;
                for (float x = step; x <= SweepLength; x += step)
                {
                    float current = model.Sample(x).LateralZ;
                    Assert.LessOrEqual(Math.Abs(current - previous), maxSlope * step + 0.05f,
                        $"Lateral jumped like a zigzag at seed={seed}, x={x}");
                    previous = current;
                }
            }
        }

        [Test]
        public void Tiers_AreAlwaysOnTheAuthoredGrid()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            var model = CreateModel(seed: 3, settings);

            for (float x = 0f; x <= SweepLength; x += 5f)
            {
                var sample = model.Sample(x);
                Assert.GreaterOrEqual(sample.TierIndex, 0);
                Assert.Less(sample.TierIndex, settings.TierCount);
                Assert.AreEqual(sample.TierIndex * settings.TierStep, sample.TierY, FloatTolerance);
            }
        }

        [Test]
        public void ConsecutivePlatforms_DifferByAtMostOneTier()
        {
            // The default tier wavelength is chosen so the quantized swell can cross at most one
            // tier boundary per platform pitch (worst realistic pitch ~20 world units).
            const float platformPitch = 20f;
            var settings = BiomeLandscapeSettings.CreateDefault();

            for (int seed = 0; seed < 10; seed++)
            {
                var model = CreateModel(seed, settings);
                int previous = model.Sample(0f).TierIndex;
                for (float x = platformPitch; x <= SweepLength; x += platformPitch)
                {
                    int current = model.Sample(x).TierIndex;
                    Assert.LessOrEqual(Math.Abs(current - previous), 1,
                        $"Tier stepped by more than one level at seed={seed}, x={x}");
                    previous = current;
                }
            }
        }

        [Test]
        public void Landmarks_WindowedScan_MatchesTheWholeSpanScan()
        {
            var model = CreateModel(seed: 9);

            var whole = model.GetLandmarksInRange(0f, SweepLength).Select(l => l.X).ToList();

            var windowed = new List<float>();
            const float windowLength = 137f; // deliberately not a multiple of the slot length
            for (float from = 0f; from < SweepLength; from += windowLength)
            {
                float to = Math.Min(from + windowLength, SweepLength);
                windowed.AddRange(model.GetLandmarksInRange(from, to).Select(l => l.X));
            }

            CollectionAssert.AreEqual(whole, windowed,
                "Contiguous windows must emit every landmark exactly once, in order.");
        }

        [Test]
        public void Landmarks_SitBehindTheLocalTrail()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            for (int seed = 0; seed < 5; seed++)
            {
                var model = CreateModel(seed, settings);
                foreach (var landmark in model.GetLandmarksInRange(0f, SweepLength))
                {
                    float from = landmark.X - settings.ArcHalfLengthMax;
                    float to = landmark.X + settings.ArcHalfLengthMax;
                    for (float x = from; x <= to; x += 1f)
                    {
                        Assert.Greater(landmark.Z, model.Sample(x).LateralZ,
                            $"Landmark at x={landmark.X} is not behind the trail at x={x} (seed={seed}).");
                    }
                }
            }
        }

        [Test]
        public void Landmarks_ScaleAndKitIndexStayInRange()
        {
            var settings = BiomeLandscapeSettings.CreateDefault();
            var model = CreateModel(seed: 11, settings);
            var landmarks = model.GetLandmarksInRange(0f, SweepLength);

            Assert.IsNotEmpty(landmarks, "Default settings over a long span should produce landmarks.");
            foreach (var landmark in landmarks)
            {
                Assert.GreaterOrEqual(landmark.Scale, settings.LandmarkScaleMin - FloatTolerance);
                Assert.LessOrEqual(landmark.Scale, settings.LandmarkScaleMax + FloatTolerance);
                Assert.GreaterOrEqual(landmark.KitIndex, 0);
            }
        }

        [Test]
        public void EmptyOrInvertedWindow_YieldsNoLandmarks()
        {
            var model = CreateModel(seed: 5);
            Assert.IsEmpty(model.GetLandmarksInRange(100f, 100f));
            Assert.IsEmpty(model.GetLandmarksInRange(200f, 100f));
        }
    }
}
