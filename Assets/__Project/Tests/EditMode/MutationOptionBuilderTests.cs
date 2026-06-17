using System.Collections.Generic;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MutationOptionBuilderTests
    {
        private readonly MutationOptionBuilder _builder = new MutationOptionBuilder();

        // RarityWeight 0.5, RarityUnlockPointsPerTier 10 (the shipped MutationConfig defaults).
        private static readonly MutationScoringParameters DefaultScoring =
            new MutationScoringParameters(0.5f, 10f);

        private static MutationCandidatePart Candidate(
            string slot, string part, int rarityTier, params (string archetype, float weight)[] affinity)
        {
            var map = new Dictionary<string, float>();
            string dominant = null;
            var best = float.NegativeInfinity;
            foreach (var (archetype, weight) in affinity)
            {
                map[archetype] = weight;
                if (weight > best)
                {
                    best = weight;
                    dominant = archetype;
                }
            }

            return new MutationCandidatePart(slot, part, part, map, rarityTier, dominant);
        }

        private static Dictionary<string, float> Tally(params (string archetype, float weight)[] entries)
        {
            var map = new Dictionary<string, float>();
            foreach (var (archetype, weight) in entries)
            {
                map[archetype] = weight;
            }

            return map;
        }

        private static HashSet<string> Equipped(params string[] partIds)
        {
            return new HashSet<string>(partIds);
        }

        private static List<string> ToPartIds(IReadOnlyList<MutationOption> options)
        {
            var ids = new List<string>(options.Count);
            foreach (var option in options)
            {
                ids.Add(option.PartId);
            }

            return ids;
        }

        [Test]
        public void Build_OrdersByAffinityMatchAgainstTally()
        {
            var tally = Tally(("reptile", 3f), ("aquatic", 1f));
            var candidates = new[]
            {
                Candidate("slot.head", "part.aquatic", 0, ("aquatic", 1f)),
                Candidate("slot.head", "part.reptile", 0, ("reptile", 1f)),
            };

            var result = _builder.Build(tally, candidates, Equipped(), 3, DefaultScoring);

            CollectionAssert.AreEqual(new[] { "part.reptile", "part.aquatic" }, ToPartIds(result));
        }

        [Test]
        public void Build_RarerPartWinsWithEqualAffinity()
        {
            var tally = Tally(("reptile", 10f));
            var candidates = new[]
            {
                Candidate("slot.head", "part.common", 0, ("reptile", 1f)),
                Candidate("slot.head", "part.rare", 2, ("reptile", 1f)),
            };

            var result = _builder.Build(tally, candidates, Equipped(), 3, DefaultScoring);

            Assert.AreEqual("part.rare", result[0].PartId);
        }

        [Test]
        public void Build_RarePartOvertakesCommonAsPointsAccumulate()
        {
            // Common has the higher affinity; the lower-affinity rare part only wins once enough
            // archetype points have been fed to ramp its rarity multiplier.
            var common = Candidate("slot.head", "part.common", 0, ("reptile", 1.0f));
            var rare = Candidate("slot.head", "part.rare", 2, ("reptile", 0.7f));
            var candidates = new[] { common, rare };

            var fewPoints = _builder.Build(
                Tally(("reptile", 4f)), candidates, Equipped(), 2, DefaultScoring);
            Assert.AreEqual("part.common", fewPoints[0].PartId, "common should lead at low points");

            var manyPoints = _builder.Build(
                Tally(("reptile", 20f)), candidates, Equipped(), 2, DefaultScoring);
            Assert.AreEqual("part.rare", manyPoints[0].PartId, "rare should overtake at high points");
        }

        [Test]
        public void Build_ExcludesEquippedParts()
        {
            var tally = Tally(("reptile", 1f));
            var candidates = new[]
            {
                Candidate("slot.head", "part.head.r", 0, ("reptile", 1f)),
                Candidate("slot.tail", "part.tail.r", 0, ("reptile", 1f)),
            };

            var result = _builder.Build(tally, candidates, Equipped("part.head.r"), 3, DefaultScoring);

            CollectionAssert.AreEqual(new[] { "part.tail.r" }, ToPartIds(result));
        }

        [Test]
        public void Build_CapsAtMaxOptions()
        {
            var tally = Tally(("reptile", 5f));
            var candidates = new[]
            {
                Candidate("slot.a", "part.a", 0, ("reptile", 0.9f)),
                Candidate("slot.b", "part.b", 0, ("reptile", 0.8f)),
                Candidate("slot.c", "part.c", 0, ("reptile", 0.7f)),
            };

            var result = _builder.Build(tally, candidates, Equipped(), 2, DefaultScoring);

            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEqual(new[] { "part.a", "part.b" }, ToPartIds(result));
        }

        [Test]
        public void Build_DropsPartsWithNoAffinityToFedArchetypes()
        {
            var tally = Tally(("reptile", 5f));
            var candidates = new[]
            {
                Candidate("slot.head", "part.reptile", 0, ("reptile", 1f)),
                Candidate("slot.tail", "part.aquatic", 5, ("aquatic", 1f)), // rare, but nothing aquatic fed
            };

            var result = _builder.Build(tally, candidates, Equipped(), 3, DefaultScoring);

            CollectionAssert.AreEqual(new[] { "part.reptile" }, ToPartIds(result));
        }

        [Test]
        public void Build_TieBreaksByOrdinalPartId()
        {
            var tally = Tally(("reptile", 2f));
            var candidates = new[]
            {
                Candidate("slot.b", "part.zzz", 0, ("reptile", 1f)),
                Candidate("slot.a", "part.aaa", 0, ("reptile", 1f)),
            };

            var result = _builder.Build(tally, candidates, Equipped(), 3, DefaultScoring);

            CollectionAssert.AreEqual(new[] { "part.aaa", "part.zzz" }, ToPartIds(result));
        }

        [Test]
        public void Build_IsDeterministicAcrossRuns()
        {
            var tally = Tally(("reptile", 3f), ("insect", 2f));
            var candidates = new[]
            {
                Candidate("slot.head", "part.head.r", 1, ("reptile", 1f)),
                Candidate("slot.tail", "part.tail.i", 1, ("insect", 1f)),
            };

            var first = ToPartIds(_builder.Build(tally, candidates, Equipped(), 3, DefaultScoring));
            var second = ToPartIds(_builder.Build(tally, candidates, Equipped(), 3, DefaultScoring));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void Build_NonPositiveMax_ReturnsEmpty()
        {
            var tally = Tally(("reptile", 1f));
            var candidates = new[] { Candidate("slot.head", "part.head.r", 0, ("reptile", 1f)) };

            CollectionAssert.IsEmpty(_builder.Build(tally, candidates, Equipped(), 0, DefaultScoring));
            CollectionAssert.IsEmpty(_builder.Build(tally, candidates, Equipped(), -1, DefaultScoring));
        }

        [Test]
        public void Build_NullArguments_ReturnEmpty()
        {
            var tally = Tally(("reptile", 1f));
            var candidates = new[] { Candidate("slot.head", "part.head.r", 0, ("reptile", 1f)) };

            CollectionAssert.IsEmpty(_builder.Build(null, candidates, Equipped(), 3, DefaultScoring));
            CollectionAssert.IsEmpty(_builder.Build(tally, null, Equipped(), 3, DefaultScoring));
        }

        [Test]
        public void Build_EmptyTally_ReturnsEmpty()
        {
            var candidates = new[] { Candidate("slot.head", "part.head.r", 2, ("reptile", 1f)) };

            var result = _builder.Build(
                new Dictionary<string, float>(), candidates, Equipped(), 3, DefaultScoring);

            CollectionAssert.IsEmpty(result);
        }
    }
}
