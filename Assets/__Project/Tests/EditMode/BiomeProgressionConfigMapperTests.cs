using System.Collections.Generic;
using System.Reflection;
using LevelGeneration;
using LevelGeneration.Journey;
using NUnit.Framework;
using UnityEngine;
using World.Biomes.Data;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeProgressionConfigMapperTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private BiomeProgressionConfig Config(params BiomeProgressionConfig.Entry[] entries)
        {
            var config = ScriptableObject.CreateInstance<BiomeProgressionConfig>();
            _created.Add(config);
            typeof(BiomeProgressionConfig).GetField("_biomes", PrivateInstance)
                .SetValue(config, new List<BiomeProgressionConfig.Entry>(entries));
            return config;
        }

        private static BiomeProgressionConfig.Entry Entry(
            LevelTheme theme, int tier, int weight, int min, int max)
        {
            var entry = new BiomeProgressionConfig.Entry();
            var type = typeof(BiomeProgressionConfig.Entry);
            type.GetField("_theme", PrivateInstance).SetValue(entry, theme);
            type.GetField("_escalationTier", PrivateInstance).SetValue(entry, tier);
            type.GetField("_selectionWeight", PrivateInstance).SetValue(entry, weight);
            type.GetField("_stretchMinWindows", PrivateInstance).SetValue(entry, min);
            type.GetField("_stretchMaxWindows", PrivateInstance).SetValue(entry, max);
            return entry;
        }

        [Test]
        public void Null_ReturnsDefaultForestSettings()
        {
            var settings = BiomeProgressionConfigMapper.ToSettings(null);

            Assert.AreEqual(1, settings.Entries.Count);
            Assert.AreEqual(LevelTheme.Forest, settings.Entries[0].Theme);
            Assert.AreEqual(1, settings.Entries[0].EscalationTier);
            Assert.Greater(settings.Entries[0].SelectionWeight, 0);
        }

        [Test]
        public void MapsAllFields()
        {
            var config = Config(Entry(LevelTheme.Mountain, tier: 2, weight: 3, min: 2, max: 5));

            var settings = BiomeProgressionConfigMapper.ToSettings(config);

            Assert.AreEqual(1, settings.Entries.Count);
            var mapped = settings.Entries[0];
            Assert.AreEqual(LevelTheme.Mountain, mapped.Theme);
            Assert.AreEqual(2, mapped.EscalationTier);
            Assert.AreEqual(3, mapped.SelectionWeight);
            Assert.AreEqual(2, mapped.StretchMinWindows);
            Assert.AreEqual(5, mapped.StretchMaxWindows);
        }

        [Test]
        public void InvertedStretchRange_Normalized()
        {
            var config = Config(Entry(LevelTheme.Forest, tier: 1, weight: 1, min: 6, max: 2));

            var settings = BiomeProgressionConfigMapper.ToSettings(config);

            Assert.AreEqual(2, settings.Entries[0].StretchMinWindows);
            Assert.AreEqual(6, settings.Entries[0].StretchMaxWindows);
        }

        [Test]
        public void DuplicateTheme_FirstWins()
        {
            var config = Config(
                Entry(LevelTheme.Forest, tier: 1, weight: 1, min: 3, max: 4),
                Entry(LevelTheme.Forest, tier: 9, weight: 9, min: 1, max: 1));

            var settings = BiomeProgressionConfigMapper.ToSettings(config);

            Assert.AreEqual(1, settings.Entries.Count);
            Assert.AreEqual(1, settings.Entries[0].EscalationTier);
            Assert.AreEqual(1, settings.Entries[0].SelectionWeight);
        }

        [Test]
        public void ZeroWeightEntry_PassesThrough()
        {
            // Weight 0 is the data-driven exclusion — the mapper must keep it, the journey skips it.
            var config = Config(Entry(LevelTheme.Cave, tier: 2, weight: 0, min: 3, max: 4));

            var settings = BiomeProgressionConfigMapper.ToSettings(config);

            Assert.AreEqual(1, settings.Entries.Count);
            Assert.AreEqual(0, settings.Entries[0].SelectionWeight);
        }
    }
}
