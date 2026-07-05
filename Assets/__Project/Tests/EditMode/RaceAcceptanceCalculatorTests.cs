using Core.Logging;
using LevelGeneration;
using NUnit.Framework;
using World.Races.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class RaceAcceptanceCalculatorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public int Warnings;
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings++;
            public void Error(LogCategory category, string message) { }
        }

        private static IRaceRoster Roster(params string[] ids)
        {
            var races = new RaceData[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                races[i] = new RaceData(ids[i], ids[i], LevelTheme.Forest);
            }

            return new RaceRoster(races);
        }

        [Test]
        public void NoMarkers_EveryRosterRace_TierZero()
        {
            var tiers = RaceAcceptanceCalculator.ComputeTiers(
                new[] { "", null, "" }, Roster("ibex", "lizard", "fox"));

            Assert.AreEqual(3, tiers.Count);
            Assert.AreEqual(0, tiers["ibex"]);
            Assert.AreEqual(0, tiers["lizard"]);
            Assert.AreEqual(0, tiers["fox"]);
        }

        [Test]
        public void TierClampsAtKin_ZeroOneTwoThreeParts()
        {
            var roster = Roster("fox");

            Assert.AreEqual(0, RaceAcceptanceCalculator.ComputeTiers(new string[0], roster)["fox"]);
            Assert.AreEqual(1, RaceAcceptanceCalculator.ComputeTiers(new[] { "fox" }, roster)["fox"]);
            Assert.AreEqual(2, RaceAcceptanceCalculator.ComputeTiers(new[] { "fox", "fox" }, roster)["fox"]);
            Assert.AreEqual(2, RaceAcceptanceCalculator.ComputeTiers(new[] { "fox", "fox", "fox" }, roster)["fox"]);
        }

        [Test]
        public void TwoRaces_OnePartEach_BothTolerated_NoExclusion()
        {
            var tiers = RaceAcceptanceCalculator.ComputeTiers(
                new[] { "fox", "lizard" }, Roster("ibex", "lizard", "fox"));

            Assert.AreEqual(1, tiers["fox"]);
            Assert.AreEqual(1, tiers["lizard"]);
            Assert.AreEqual(0, tiers["ibex"]);
        }

        [Test]
        public void UnknownRaceId_Ignored_Warns()
        {
            var logger = new FakeLogger();

            var tiers = RaceAcceptanceCalculator.ComputeTiers(
                new[] { "wolf", "fox" }, Roster("fox"), logger);

            Assert.AreEqual(1, tiers["fox"]);
            Assert.IsFalse(tiers.ContainsKey("wolf"));
            Assert.AreEqual(1, logger.Warnings);
        }

        [Test]
        public void SameBody_SameTiers_Deterministic()
        {
            var roster = Roster("ibex", "lizard", "fox");
            var body = new[] { "fox", "", "lizard", "fox" };

            var first = RaceAcceptanceCalculator.ComputeTiers(body, roster);
            var second = RaceAcceptanceCalculator.ComputeTiers(body, roster);

            CollectionAssert.AreEquivalent(first, second);
            Assert.AreEqual(2, first["fox"]);
            Assert.AreEqual(1, first["lizard"]);
            Assert.AreEqual(0, first["ibex"]);
        }

        [Test]
        public void NullRoster_ReturnsEmpty()
        {
            var tiers = RaceAcceptanceCalculator.ComputeTiers(new[] { "fox" }, null);

            Assert.AreEqual(0, tiers.Count);
        }
    }
}
