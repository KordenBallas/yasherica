using System.Collections.Generic;
using System.Reflection;
using Core.Logging;
using LevelGeneration;
using NUnit.Framework;
using UnityEngine;
using World.Races.Data;

namespace Tests.EditMode
{
    [TestFixture]
    public class RaceRosterMapperTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public int Warnings;
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings++;
            public void Error(LogCategory category, string message) { }
        }

        private readonly List<Object> _created = new List<Object>();
        private FakeLogger _logger;

        [SetUp]
        public void SetUp() => _logger = new FakeLogger();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        private RaceDefinition Race(string id, string displayName = "Name", LevelTheme biome = LevelTheme.Forest)
        {
            var race = ScriptableObject.CreateInstance<RaceDefinition>();
            _created.Add(race);
            Set(race, "_raceId", id);
            Set(race, "_displayName", displayName);
            Set(race, "_homeBiome", biome);
            return race;
        }

        private static void Set(RaceDefinition race, string field, object value) =>
            typeof(RaceDefinition).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(race, value);

        [Test]
        public void Null_ReturnsEmptyRoster_Warns()
        {
            var roster = RaceRosterMapper.ToRoster(null, _logger);

            Assert.AreEqual(0, roster.All.Count);
            Assert.AreEqual(1, _logger.Warnings);
        }

        [Test]
        public void MapsAllFields_InAuthoredOrder()
        {
            var roster = RaceRosterMapper.ToRoster(new[]
            {
                Race("ibex", "Ibex-folk", LevelTheme.Mountain),
                Race("fox", "Fox-folk", LevelTheme.Forest)
            }, _logger);

            Assert.AreEqual(2, roster.All.Count);
            Assert.AreEqual("ibex", roster.All[0].Id);
            Assert.AreEqual("Ibex-folk", roster.All[0].DisplayName);
            Assert.AreEqual(LevelTheme.Mountain, roster.All[0].HomeBiome);
            Assert.AreEqual("fox", roster.All[1].Id);
            Assert.IsTrue(roster.TryGet("fox", out var fox));
            Assert.AreEqual(LevelTheme.Forest, fox.HomeBiome);
        }

        [Test]
        public void EmptyId_Skipped_Warns()
        {
            var roster = RaceRosterMapper.ToRoster(new[] { Race(""), Race("fox") }, _logger);

            Assert.AreEqual(1, roster.All.Count);
            Assert.AreEqual("fox", roster.All[0].Id);
            Assert.AreEqual(1, _logger.Warnings);
        }

        [Test]
        public void DuplicateId_FirstAuthoredWins_Warns()
        {
            var roster = RaceRosterMapper.ToRoster(new[]
            {
                Race("fox", "First"),
                Race("fox", "Second")
            }, _logger);

            Assert.AreEqual(1, roster.All.Count);
            Assert.AreEqual("First", roster.All[0].DisplayName);
            Assert.AreEqual(1, _logger.Warnings);
        }

        [Test]
        public void EmptyDisplayName_FallsBackToAssetName()
        {
            var race = Race("fox", "");
            race.name = "Race_Fox";

            var roster = RaceRosterMapper.ToRoster(new[] { race }, _logger);

            Assert.AreEqual("Race_Fox", roster.All[0].DisplayName);
        }

        [Test]
        public void UnknownId_ContainsAndTryGet_ReturnFalse()
        {
            var roster = RaceRosterMapper.ToRoster(new[] { Race("fox") }, _logger);

            Assert.IsFalse(roster.Contains("ibex"));
            Assert.IsFalse(roster.TryGet("ibex", out _));
            Assert.IsFalse(roster.Contains(null));
        }
    }
}
