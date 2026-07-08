using System.Collections.Generic;
using Core.Persistence;
using MetaProgression.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DirectionTallyTests
    {
        private static readonly IReadOnlyDictionary<string, string> RaceMap =
            new Dictionary<string, string>
            {
                { "part_ibex", "ibex" },
                { "part_fox", "fox" }
            };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> TraitMap =
            new Dictionary<string, IReadOnlyList<string>>
            {
                { "art_spark", new[] { "spark" } },
                { "art_wet_rot", new[] { "wet", "rot" } }
            };

        private static MetaProgressionSettings Settings(
            int window = 4, float raceWeight = 1f, float artifactWeight = 1f) =>
            new MetaProgressionSettings(null, window, raceWeight, artifactWeight, 1f, 0.75f, 3, false, 1f, 8);

        private static MetaRunLedgerSnapshot Ledger(params RunLedgerEntryDto[] runs)
        {
            var ledger = new MetaRunLedgerSnapshot();
            ledger.Runs.AddRange(runs);
            return ledger;
        }

        private static RunLedgerEntryDto Run(int index, string[] parts = null, string[] artifacts = null) =>
            new RunLedgerEntryDto
            {
                RunIndex = index,
                InstalledPartIds = new List<string>(parts ?? new string[0]),
                SocketedArtifactIds = new List<string>(artifacts ?? new string[0])
            };

        [Test]
        public void EmptyOrNullLedger_IsNeutral()
        {
            Assert.IsTrue(DirectionTally.Compute(null, Settings(), RaceMap, TraitMap).IsNeutral);
            Assert.IsTrue(DirectionTally.Compute(Ledger(), Settings(), RaceMap, TraitMap).IsNeutral);
        }

        [Test]
        public void TalliesBothAxes_AndNormalizesToStrongestSignal()
        {
            var ledger = Ledger(
                Run(1, parts: new[] { "part_ibex" }, artifacts: new[] { "art_spark" }),
                Run(2, parts: new[] { "part_ibex" }));

            var profile = DirectionTally.Compute(ledger, Settings(), RaceMap, TraitMap);

            // Ibex tallied twice (the strongest signal → 1), spark once (→ 0.5).
            Assert.AreEqual(1f, profile.ScoreFor("ibex", null), 1e-4);
            Assert.AreEqual(0.5f, profile.ScoreFor(null, new[] { "spark" }), 1e-4);
            Assert.AreEqual(0f, profile.ScoreFor("fox", null), 1e-4);
        }

        [Test]
        public void WindowSlides_OldRunsFallOut()
        {
            // Ibex pursued in runs 1-2, fox in runs 5-6; window 2 anchored at run 6 sees only fox.
            var ledger = Ledger(
                Run(1, parts: new[] { "part_ibex" }),
                Run(2, parts: new[] { "part_ibex" }),
                Run(5, parts: new[] { "part_fox" }),
                Run(6, parts: new[] { "part_fox" }));

            var profile = DirectionTally.Compute(ledger, Settings(window: 2), RaceMap, TraitMap);

            Assert.AreEqual(0f, profile.ScoreFor("ibex", null), 1e-4);
            Assert.AreEqual(1f, profile.ScoreFor("fox", null), 1e-4);
        }

        [Test]
        public void AxisWeights_ShiftTheBalance()
        {
            var ledger = Ledger(Run(1, parts: new[] { "part_ibex" }, artifacts: new[] { "art_spark" }));

            var raceHeavy = DirectionTally.Compute(ledger, Settings(raceWeight: 2f, artifactWeight: 1f), RaceMap, TraitMap);

            Assert.AreEqual(1f, raceHeavy.ScoreFor("ibex", null), 1e-4);
            Assert.AreEqual(0.5f, raceHeavy.ScoreFor(null, new[] { "spark" }), 1e-4);
        }

        [Test]
        public void UnknownIds_AreSkipped()
        {
            var ledger = Ledger(Run(1,
                parts: new[] { "part_removed", "part_ibex" },
                artifacts: new[] { "art_removed" }));

            var profile = DirectionTally.Compute(ledger, Settings(), RaceMap, TraitMap);

            Assert.IsFalse(profile.IsNeutral);
            Assert.AreEqual(1f, profile.ScoreFor("ibex", null), 1e-4);
        }

        [Test]
        public void MultiTraitArtifact_FeedsEveryTrait()
        {
            var ledger = Ledger(Run(1, artifacts: new[] { "art_wet_rot" }));

            var profile = DirectionTally.Compute(ledger, Settings(), RaceMap, TraitMap);

            Assert.AreEqual(1f, profile.ScoreFor(null, new[] { "wet" }), 1e-4);
            Assert.AreEqual(1f, profile.ScoreFor(null, new[] { "rot" }), 1e-4);
        }

        [Test]
        public void Deterministic_SameInputsSameProfile()
        {
            var ledger = Ledger(
                Run(1, parts: new[] { "part_ibex" }, artifacts: new[] { "art_spark" }),
                Run(3, parts: new[] { "part_fox" }));

            var first = DirectionTally.Compute(ledger, Settings(), RaceMap, TraitMap);
            var second = DirectionTally.Compute(ledger, Settings(), RaceMap, TraitMap);

            foreach (var race in new[] { "ibex", "fox" })
            {
                Assert.AreEqual(first.ScoreFor(race, null), second.ScoreFor(race, null));
            }
        }
    }
}
