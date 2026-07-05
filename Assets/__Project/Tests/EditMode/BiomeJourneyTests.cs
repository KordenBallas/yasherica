using System.Collections.Generic;
using System.Linq;
using Core.Logging;
using LevelGeneration;
using LevelGeneration.Journey;
using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeJourneyTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public int Warnings;
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings++;
            public void Error(LogCategory category, string message) { }
        }

        private static BiomeProgressionEntry Entry(
            LevelTheme theme, int tier, int weight = 1, int min = 3, int max = 4) =>
            new BiomeProgressionEntry(theme, tier, weight, min, max);

        private static BiomeProgressionSettings Settings(params BiomeProgressionEntry[] entries) =>
            new BiomeProgressionSettings(new List<BiomeProgressionEntry>(entries));

        private static BiomeJourney Journey(BiomeProgressionSettings settings, ulong seed = 42) =>
            new BiomeJourney(settings, new DeterministicRandom(seed), new FakeLogger());

        /// <summary>The default three-biome authoring: Forest t1, Mountain+Desert sharing t2.</summary>
        private static BiomeProgressionSettings ThreeBiomes() => Settings(
            Entry(LevelTheme.Forest, tier: 1),
            Entry(LevelTheme.Mountain, tier: 2),
            Entry(LevelTheme.Desert, tier: 2));

        private static string Signature(IBiomeJourney journey, int windows)
        {
            var parts = new List<string>();
            for (int w = 0; w < windows; w++)
            {
                var s = journey.ForWindow(w);
                parts.Add($"{w}:{s.Theme}:{s.EscalationTier}:{s.StretchIndex}");
            }

            return string.Join("|", parts);
        }

        [Test]
        public void SameSeed_IdenticalJourney()
        {
            var a = Journey(ThreeBiomes(), seed: 42);
            var b = Journey(ThreeBiomes(), seed: 42);

            Assert.AreEqual(Signature(a, 40), Signature(b, 40));
        }

        [Test]
        public void DifferentSeeds_DivergeWithinTier()
        {
            // Two biomes share tier 2; over enough seeds, the first tier-2 pick must differ somewhere.
            var picks = new HashSet<LevelTheme>();
            for (ulong seed = 0; seed < 16; seed++)
            {
                var journey = Journey(ThreeBiomes(), seed);
                // Walk forward to the first stretch past stretch 0 (the tier-2 stretch).
                int w = 0;
                while (journey.ForWindow(w).StretchIndex == 0) w++;
                picks.Add(journey.ForWindow(w).Theme);
            }

            CollectionAssert.AreEquivalent(new[] { LevelTheme.Mountain, LevelTheme.Desert }, picks,
                "Seeds should diverge between the two tier-2 biomes.");
        }

        [Test]
        public void FirstStretch_UsesLowestAuthoredTier()
        {
            var journey = Journey(ThreeBiomes());
            var first = journey.ForWindow(0);

            Assert.AreEqual(1, first.EscalationTier);
            Assert.AreEqual(LevelTheme.Forest, first.Theme);
            Assert.AreEqual(0, first.FirstWindow);
        }

        [Test]
        public void ClimbsOneTierPerStretch_ClampsAtTop()
        {
            // Non-contiguous authored tiers {1, 2, 5}: the tier is an ordering key, not an index.
            var settings = Settings(
                Entry(LevelTheme.Forest, tier: 1),
                Entry(LevelTheme.Mountain, tier: 2),
                Entry(LevelTheme.Desert, tier: 5));
            var journey = Journey(settings);

            var tiersByStretch = new List<int>();
            for (int w = 0; w < 60; w++)
            {
                var s = journey.ForWindow(w);
                if (s.StretchIndex == tiersByStretch.Count)
                {
                    tiersByStretch.Add(s.EscalationTier);
                }
            }

            Assert.GreaterOrEqual(tiersByStretch.Count, 5);
            Assert.AreEqual(1, tiersByStretch[0]);
            Assert.AreEqual(2, tiersByStretch[1]);
            for (int i = 2; i < tiersByStretch.Count; i++)
            {
                Assert.AreEqual(5, tiersByStretch[i], $"Stretch {i} should clamp at the top tier.");
            }
        }

        [Test]
        public void StretchLengths_WithinAuthoredRange_AndContiguous()
        {
            var journey = Journey(ThreeBiomes());

            int expectedStart = 0;
            int lastStretchIndex = -1;
            for (int w = 0; w < 60; w++)
            {
                var s = journey.ForWindow(w);
                Assert.IsTrue(s.FirstWindow <= w && w < s.EndWindowExclusive,
                    $"Stretch {s.StretchIndex} must cover window {w}.");
                if (s.StretchIndex != lastStretchIndex)
                {
                    Assert.AreEqual(lastStretchIndex + 1, s.StretchIndex, "Stretches must be sequential.");
                    Assert.AreEqual(expectedStart, s.FirstWindow, "Stretches must tile the window axis.");
                    Assert.That(s.WindowCount, Is.InRange(3, 4), "Authored stretch range is 3..4.");
                    expectedStart = s.EndWindowExclusive;
                    lastStretchIndex = s.StretchIndex;
                }
            }
        }

        [Test]
        public void UnlistedTheme_NeverAppears()
        {
            // Cave has no entry — the data-driven exclusion.
            var journey = Journey(ThreeBiomes());

            for (int w = 0; w < 200; w++)
            {
                Assert.AreNotEqual(LevelTheme.Cave, journey.ForWindow(w).Theme);
            }
        }

        [Test]
        public void ZeroWeightEntry_NeverPicked()
        {
            var settings = Settings(
                Entry(LevelTheme.Forest, tier: 1),
                Entry(LevelTheme.Mountain, tier: 2),
                Entry(LevelTheme.Cave, tier: 2, weight: 0));
            var journey = Journey(settings);

            for (int w = 0; w < 200; w++)
            {
                Assert.AreNotEqual(LevelTheme.Cave, journey.ForWindow(w).Theme);
            }
        }

        [Test]
        public void NoImmediateRepeat_WhenTierOffersAlternative()
        {
            var journey = Journey(ThreeBiomes());

            LevelTheme? previous = null;
            int lastStretchIndex = -1;
            for (int w = 0; w < 200; w++)
            {
                var s = journey.ForWindow(w);
                if (s.StretchIndex != lastStretchIndex)
                {
                    if (s.StretchIndex >= 2)
                    {
                        // From stretch 1 on, the pool is the two-biome tier 2: consecutive stretches
                        // must alternate.
                        Assert.AreNotEqual(previous, s.Theme,
                            $"Stretch {s.StretchIndex} repeated {s.Theme} with an alternative available.");
                    }

                    previous = s.Theme;
                    lastStretchIndex = s.StretchIndex;
                }
            }
        }

        [Test]
        public void SingleBiomeTopTier_RepeatsWithoutError()
        {
            var settings = Settings(
                Entry(LevelTheme.Forest, tier: 1),
                Entry(LevelTheme.Desert, tier: 2));
            var journey = Journey(settings);

            for (int w = 20; w < 100; w++)
            {
                Assert.AreEqual(LevelTheme.Desert, journey.ForWindow(w).Theme,
                    "A single-biome top tier legitimately repeats forever.");
            }
        }

        [Test]
        public void EmptySettings_FallsBackToForestTier1()
        {
            var logger = new FakeLogger();
            var journey = new BiomeJourney(
                new BiomeProgressionSettings(new List<BiomeProgressionEntry>()),
                new DeterministicRandom(1), logger);

            for (int w = 0; w < 40; w++)
            {
                var s = journey.ForWindow(w);
                Assert.AreEqual(LevelTheme.Forest, s.Theme);
                Assert.AreEqual(1, s.EscalationTier);
            }

            Assert.AreEqual(1, logger.Warnings, "The fallback should warn exactly once.");
        }

        [Test]
        public void ForWindow_IdempotentAndOrderIndependent()
        {
            var sequential = Journey(ThreeBiomes(), seed: 7);
            string expected = Signature(sequential, 20);

            var outOfOrder = Journey(ThreeBiomes(), seed: 7);
            // Query far ahead first, then backwards — the plan must not depend on query order.
            outOfOrder.ForWindow(19);
            outOfOrder.ForWindow(3);
            outOfOrder.ForWindow(11);

            Assert.AreEqual(expected, Signature(outOfOrder, 20));
        }
    }
}
