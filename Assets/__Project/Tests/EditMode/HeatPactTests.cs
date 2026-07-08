using System.Collections.Generic;
using Heat.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HeatPactTests
    {
        internal static HeatSettings DemoSettings(int softCap = 0) => new HeatSettings(
            new[]
            {
                new HeatModifier("enemies-first", "They Strike First", HeatEffectKind.EnemiesActFirst,
                    new[] { new HeatRank(2, 1, "always lead") }),
                new HeatModifier("raised-floor", "Older Things Wake", HeatEffectKind.RaisedCreatureFloor,
                    new[] { new HeatRank(2, 1, "tier +1"), new HeatRank(2, 1, "tier +2") }),
                new HeatModifier("stingy-cauldron", "The Cauldron Skimps", HeatEffectKind.StingyCauldron,
                    new[] { new HeatRank(1, 1, "one fewer"), new HeatRank(2, 1, "two fewer") }),
                new HeatModifier("fewer-sockets", "Thinner Medallions", HeatEffectKind.FewerSockets,
                    new[] { new HeatRank(2, 1, "one fewer socket") })
            },
            floorReliefRunsPerHeat: 0.5f,
            biasStrengthLiftPerHeat: 0.15f,
            reserveDirectionSlotMinHeat: 4,
            softCapTotalHeat: softCap,
            clearWindowFloor: 2);

        private static KeyValuePair<string, int> Entry(string id, int rank) =>
            new KeyValuePair<string, int>(id, rank);

        [Test]
        public void TotalHeat_SumsTakenRankSteps()
        {
            var pact = HeatPact.From(DemoSettings(), new[]
            {
                Entry("raised-floor", 2), // 2 + 2
                Entry("stingy-cauldron", 1) // 1
            });

            Assert.AreEqual(5, pact.TotalHeat);
            Assert.AreEqual(2, pact.RankOf("raised-floor"));
            Assert.AreEqual(1, pact.RankOf("stingy-cauldron"));
            Assert.AreEqual(0, pact.RankOf("enemies-first"));
        }

        [Test]
        public void UnknownIds_Drop_AndRanksClamp()
        {
            // A stale save against a re-authored menu degrades to a cooler pact, never a crash (FR12).
            var pact = HeatPact.From(DemoSettings(), new[]
            {
                Entry("removed-modifier", 3),
                Entry("enemies-first", 9) // clamps to max rank 1
            });

            Assert.AreEqual(1, pact.RankOf("enemies-first"));
            Assert.AreEqual(0, pact.RankOf("removed-modifier"));
            Assert.AreEqual(2, pact.TotalHeat);
        }

        [Test]
        public void DuplicateAndZeroRankEntries_Collapse()
        {
            var pact = HeatPact.From(DemoSettings(), new[]
            {
                Entry("stingy-cauldron", 1),
                Entry("stingy-cauldron", 2), // duplicate: first wins
                Entry("fewer-sockets", 0) // untaken: absent
            });

            Assert.AreEqual(1, pact.RankOf("stingy-cauldron"));
            Assert.AreEqual(1, pact.Ranks.Count);
        }

        [Test]
        public void NoTakenEntries_YieldNone()
        {
            Assert.AreSame(HeatPact.None, HeatPact.From(DemoSettings(), new KeyValuePair<string, int>[0]));
            Assert.AreSame(HeatPact.None, HeatPact.From(null, new[] { Entry("enemies-first", 1) }));
            Assert.AreEqual(0, HeatPact.None.TotalHeat);
        }
    }
}
