using System;
using System.Collections.Generic;
using Loot.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class WeightedPickerTests
    {
        private static float Weight(LootEntryData entry) => entry.Weight;

        [Test]
        public void Pick_EmptyOrNullEntries_ReturnsNull()
        {
            var random = new Random(1);

            Assert.IsNull(WeightedPicker.Pick(null, Weight, random));
            Assert.IsNull(WeightedPicker.Pick(Array.Empty<LootEntryData>(), Weight, random));
        }

        [Test]
        public void Pick_AllZeroWeights_ReturnsNull()
        {
            var entries = new[]
            {
                new LootEntryData("fire", 0f),
                new LootEntryData("water", 0f)
            };

            Assert.IsNull(WeightedPicker.Pick(entries, Weight, new Random(1)));
        }

        [Test]
        public void Pick_SingleEntry_AlwaysPicked()
        {
            var entries = new[] { new LootEntryData("fire", 0.5f) };

            for (int seed = 0; seed < 20; seed++)
            {
                var picked = WeightedPicker.Pick(entries, Weight, new Random(seed));
                Assert.AreEqual("fire", picked.ArtifactId);
            }
        }

        [Test]
        public void Pick_ZeroWeightEntry_IsNeverPicked()
        {
            var entries = new[]
            {
                new LootEntryData("fire", 1f),
                new LootEntryData("water", 0f)
            };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = WeightedPicker.Pick(entries, Weight, new Random(seed));
                Assert.AreEqual("fire", picked.ArtifactId);
            }
        }

        [Test]
        public void Pick_RespectsWeightDistribution()
        {
            var entries = new[]
            {
                new LootEntryData("common", 9f),
                new LootEntryData("rare", 1f)
            };
            var random = new Random(42);
            var counts = new Dictionary<string, int> { ["common"] = 0, ["rare"] = 0 };

            for (int i = 0; i < 1000; i++)
            {
                counts[WeightedPicker.Pick(entries, Weight, random).ArtifactId]++;
            }

            Assert.Greater(counts["common"], counts["rare"] * 5);
            Assert.Greater(counts["rare"], 0);
        }

        [Test]
        public void Pick_SameRandomSeed_ReturnsSameEntry()
        {
            var entries = new[]
            {
                new LootEntryData("fire", 1f),
                new LootEntryData("water", 1f),
                new LootEntryData("rock", 1f)
            };

            var first = WeightedPicker.Pick(entries, Weight, new Random(7));
            var second = WeightedPicker.Pick(entries, Weight, new Random(7));

            Assert.AreSame(first, second);
        }
    }
}
