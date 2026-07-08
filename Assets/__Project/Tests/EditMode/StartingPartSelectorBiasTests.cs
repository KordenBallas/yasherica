using System.Collections.Generic;
using Hub.Core;
using MetaProgression.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The Track-R dig bias contract on <see cref="StartingPartSelector"/>: neutral settings
    /// reproduce the legacy unbiased draw exactly; bias raises a matching candidate's offer
    /// frequency across seeds; the ceiling clamps any single candidate's tie-pick share below
    /// certainty (FR9); a bigger pool dilutes a fixed candidate (FR10).
    /// </summary>
    [TestFixture]
    public class StartingPartSelectorBiasTests
    {
        private const int SeedSweep = 400;

        private static StartingPartCandidate Plain(string id, float drawWeight = 1f,
            string raceId = null, IReadOnlyList<string> traits = null)
        {
            // A unique race per candidate + one shared slot: every candidate scores the same
            // (unseen race + unseen slot + flavored) so the whole pool lands in ONE tie set and
            // the tie-break alone decides the first pick.
            return new StartingPartCandidate(id, raceId ?? ("race_" + id), "slot_shared", false, traits, drawWeight);
        }

        private static MetaProgressionSettings Settings(
            float biasStrength = 1f, float ceiling = 0.75f, float dilution = 1f) =>
            new MetaProgressionSettings(null, 4, 1f, 1f, biasStrength, ceiling, 3, false, dilution, 8);

        private static DirectionProfile Direction(params (string race, float weight)[] races)
        {
            var map = new Dictionary<string, float>();
            foreach (var (race, weight) in races)
            {
                map[race] = weight;
            }

            return new DirectionProfile(map, null);
        }

        [Test]
        public void NeutralSettings_ReproduceTheLegacyDrawExactly()
        {
            var selector = new StartingPartSelector();
            var pool = new List<StartingPartCandidate>
            {
                new StartingPartCandidate("part_a", "ibex", "arm", true),
                new StartingPartCandidate("part_b", "fox", "leg", false),
                new StartingPartCandidate("part_c", "", "tail", true),
                new StartingPartCandidate("part_d", "ibex", "leg", false),
                new StartingPartCandidate("part_e", "", "arm", false)
            };

            for (int seed = 0; seed < 50; seed++)
            {
                var legacy = selector.Draw(pool, 3, seed);
                var neutral = selector.Draw(pool, 3, seed, DirectionProfile.Neutral, Settings(biasStrength: 0f));
                var nullArgs = selector.Draw(pool, 3, seed, null, null);

                for (int i = 0; i < legacy.Count; i++)
                {
                    Assert.AreEqual(legacy[i].PartId, neutral[i].PartId, $"seed {seed}, pick {i}");
                    Assert.AreEqual(legacy[i].PartId, nullArgs[i].PartId, $"seed {seed}, pick {i}");
                }
            }
        }

        [Test]
        public void Bias_RaisesAMatchingCandidatesOfferFrequency()
        {
            var selector = new StartingPartSelector();
            // One-slot-tie pool: all plain, so the tie-break decides the single pick.
            var pool = new List<StartingPartCandidate>
            {
                Plain("part_spark", raceId: "spark_kin"),
                Plain("part_a", raceId: "other_a"),
                Plain("part_b", raceId: "other_b"),
                Plain("part_c", raceId: "other_c")
            };
            var direction = Direction(("spark_kin", 1f));

            int unbiasedHits = CountFirstPick(selector, pool, "part_spark", null, null);
            int biasedHits = CountFirstPick(selector, pool, "part_spark", direction, Settings(biasStrength: 4f));

            Assert.Greater(biasedHits, unbiasedHits);
        }

        [Test]
        public void Ceiling_ClampsAnySingleCandidateBelowCertainty()
        {
            var selector = new StartingPartSelector();
            var pool = new List<StartingPartCandidate>
            {
                Plain("part_hot", drawWeight: 10000f, raceId: "hot_kin"),
                Plain("part_a"),
                Plain("part_b")
            };
            var direction = Direction(("hot_kin", 1f));
            var settings = Settings(biasStrength: 1000f, ceiling: 0.75f);

            int hits = CountFirstPick(selector, pool, "part_hot", direction, settings);

            // Extreme weight + bias: share must stay near (and never above ~) the 0.75 ceiling.
            Assert.Less(hits, SeedSweep, "an extreme configuration must never reach certainty (FR9)");
            Assert.LessOrEqual(hits, (int)(SeedSweep * 0.80f), "share should be clamped to ~ the ceiling");
            Assert.Greater(hits, (int)(SeedSweep * 0.55f), "the favourite should still lead");
        }

        [Test]
        public void Ceiling_HoldsWithSeveralDominantWeights()
        {
            var selector = new StartingPartSelector();
            var pool = new List<StartingPartCandidate>
            {
                Plain("part_h1", drawWeight: 1000f),
                Plain("part_h2", drawWeight: 1000f),
                Plain("part_tiny")
            };
            // c = 0.4 < 0.5: with two equal dominants, each must clamp to ≤ 40%-ish of the draw.
            var settings = Settings(biasStrength: 0f, ceiling: 0.4f);

            int h1 = CountFirstPick(selector, pool, "part_h1", DirectionProfile.Neutral, settings);

            Assert.LessOrEqual(h1, (int)(SeedSweep * 0.46f));
        }

        [Test]
        public void Dilution_BiggerPoolLowersAFixedCandidatesFrequency()
        {
            var selector = new StartingPartSelector();
            var direction = Direction(("spark_kin", 1f));
            var settings = Settings(biasStrength: 2f);

            var smallPool = new List<StartingPartCandidate>
            {
                Plain("part_spark", raceId: "spark_kin"),
                Plain("part_a"),
                Plain("part_b")
            };
            var bigPool = new List<StartingPartCandidate>(smallPool)
            {
                Plain("part_c"),
                Plain("part_d"),
                Plain("part_e"),
                Plain("part_f")
            };

            int smallHits = CountFirstPick(selector, smallPool, "part_spark", direction, settings);
            int bigHits = CountFirstPick(selector, bigPool, "part_spark", direction, settings);

            Assert.Less(bigHits, smallHits);
        }

        [Test]
        public void DilutionExponent_SharpensTheFavourite()
        {
            var selector = new StartingPartSelector();
            var direction = Direction(("spark_kin", 1f));
            var pool = new List<StartingPartCandidate>
            {
                Plain("part_spark", raceId: "spark_kin"),
                Plain("part_a"),
                Plain("part_b"),
                Plain("part_c")
            };

            int flat = CountFirstPick(selector, pool, "part_spark", direction,
                Settings(biasStrength: 1f, ceiling: 0.95f, dilution: 1f));
            int sharp = CountFirstPick(selector, pool, "part_spark", direction,
                Settings(biasStrength: 1f, ceiling: 0.95f, dilution: 3f));

            Assert.Greater(sharp, flat);
        }

        [Test]
        public void ReserveDirectionSlot_GuaranteesAMatchWhenPoolHasOne()
        {
            var selector = new StartingPartSelector();
            // Distinct races/slots so the variety draw fills up before reaching the spark part.
            var pool = new List<StartingPartCandidate>
            {
                new StartingPartCandidate("part_a", "race_a", "slot_a", true),
                new StartingPartCandidate("part_b", "race_b", "slot_b", true),
                new StartingPartCandidate("part_c", "race_c", "slot_c", true),
                new StartingPartCandidate("part_z_spark", "spark_kin", "slot_a", false)
            };
            var direction = Direction(("spark_kin", 1f));
            var reserveOn = new MetaProgressionSettings(null, 4, 1f, 1f, 0f, 0.75f, 3, true, 1f, 8);

            for (int seed = 0; seed < 40; seed++)
            {
                var offer = selector.Draw(pool, 3, seed, direction, reserveOn);
                bool hasMatch = false;
                foreach (var pick in offer)
                {
                    hasMatch |= pick.RaceId == "spark_kin";
                }

                Assert.IsTrue(hasMatch, $"seed {seed}: the floor rule must surface the direction");
            }
        }

        [Test]
        public void SameSeedAndInputs_GiveIdenticalBiasedOffers()
        {
            var selector = new StartingPartSelector();
            var pool = new List<StartingPartCandidate>
            {
                Plain("part_spark", raceId: "spark_kin"),
                Plain("part_a"),
                Plain("part_b")
            };
            var direction = Direction(("spark_kin", 1f));
            var settings = Settings(biasStrength: 2f);

            var first = selector.Draw(pool, 2, 42, direction, settings);
            var second = selector.Draw(pool, 2, 42, direction, settings);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].PartId, second[i].PartId);
            }
        }

        private static int CountFirstPick(
            StartingPartSelector selector, List<StartingPartCandidate> pool, string partId,
            DirectionProfile direction, MetaProgressionSettings settings)
        {
            int hits = 0;
            for (int seed = 0; seed < SeedSweep; seed++)
            {
                var offer = selector.Draw(pool, 1, seed, direction, settings);
                if (offer.Count > 0 && offer[0].PartId == partId)
                {
                    hits++;
                }
            }

            return hits;
        }
    }
}
