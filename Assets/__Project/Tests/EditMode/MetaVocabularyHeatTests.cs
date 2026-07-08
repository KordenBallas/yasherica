using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The Heat lens on the vocabulary (heat-ascension FR5/FR6): min-Heat gates keyed to the
    /// current pact or the high-water record, tier run-floor relief, and — the safety net — a
    /// vocabulary WITHOUT the lens answering byte-identically to before Track Y.
    /// </summary>
    [TestFixture]
    public class MetaVocabularyHeatTests
    {
        private sealed class StubHeatLens : IHeatLens
        {
            public int CurrentHeat { get; set; }
            public int HighWaterHeat { get; set; }
            public int FloorReliefRuns { get; set; }
        }

        private static MetaProgressionSettings SettingsWithFloors(params int[] floors) =>
            new MetaProgressionSettings(floors, 4, 1f, 1f, 1f, 0.75f, 3, false, 1f, 8);

        private static MetaGate HeatGate(int minHeat, HeatGateKey key, int tier = 0) =>
            new MetaGate(GatingMark.MetaGated, null, tier, 1f, minHeat, key);

        [Test]
        public void MinHeat_CurrentPactKey_ComparesTheCurrentPact()
        {
            var settings = SettingsWithFloors(0);
            var gate = HeatGate(3, HeatGateKey.CurrentPact);

            Assert.IsFalse(new MetaVocabulary(null, settings, 5,
                new StubHeatLens { CurrentHeat = 2, HighWaterHeat = 9 }).IsUnlocked("t", gate),
                "a hot record does not satisfy a current-pact gate");
            Assert.IsTrue(new MetaVocabulary(null, settings, 5,
                new StubHeatLens { CurrentHeat = 3 }).IsUnlocked("t", gate));
        }

        [Test]
        public void MinHeat_HighWaterKey_ComparesTheRecord()
        {
            var settings = SettingsWithFloors(0);
            var gate = HeatGate(3, HeatGateKey.HighWaterMark);

            Assert.IsFalse(new MetaVocabulary(null, settings, 5,
                new StubHeatLens { CurrentHeat = 9, HighWaterHeat = 2 }).IsUnlocked("t", gate),
                "a hot current pact does not satisfy a high-water gate");
            Assert.IsTrue(new MetaVocabulary(null, settings, 5,
                new StubHeatLens { HighWaterHeat = 3 }).IsUnlocked("t", gate));
        }

        [Test]
        public void MinHeatGate_FailsClosedWithoutTheLens()
        {
            var vocabulary = new MetaVocabulary(null, SettingsWithFloors(0), 5);

            Assert.IsFalse(vocabulary.IsUnlocked("t", HeatGate(1, HeatGateKey.CurrentPact)));
            Assert.IsFalse(vocabulary.IsUnlocked("t", HeatGate(1, HeatGateKey.HighWaterMark)));
        }

        [Test]
        public void FloorRelief_OpensATierEarly_NeverBelowZero()
        {
            var settings = SettingsWithFloors(1, 5);
            var gate = new MetaGate(GatingMark.MetaGated, null, 1, 1f);

            Assert.IsFalse(new MetaVocabulary(null, settings, 3).IsUnlocked("t", gate),
                "floor 5 holds at run 3 without heat");
            Assert.IsTrue(new MetaVocabulary(null, settings, 3,
                new StubHeatLens { FloorReliefRuns = 2 }).IsUnlocked("t", gate),
                "relief 2 lowers the effective floor to 3");
            Assert.IsTrue(new MetaVocabulary(null, settings, 1,
                new StubHeatLens { FloorReliefRuns = 100 }).IsUnlocked("t", gate),
                "absurd relief floors at 0, never negative");
        }

        [Test]
        public void NullLens_AnswersIdenticallyToBeforeTrackY()
        {
            // The Heat-0 zero-diff guarantee (FR13): gates without heat data behave exactly as
            // the pre-Track-Y vocabulary for any (tier, run) combination.
            var settings = SettingsWithFloors(1, 3, 7);
            var plainGate0 = new MetaGate(GatingMark.MetaGated, null, 0, 1f);
            var plainGate2 = new MetaGate(GatingMark.MetaGated, null, 2, 1f);
            var withNullLens = new MetaVocabulary(null, settings, 4, null);
            var legacy = new MetaVocabulary(null, settings, 4);

            foreach (var gate in new[] { plainGate0, plainGate2, MetaGate.Base })
            {
                Assert.AreEqual(legacy.IsUnlocked("t", gate), withNullLens.IsUnlocked("t", gate));
            }
        }

        [Test]
        public void BaseTokens_IgnoreHeatEntirely()
        {
            var vocabulary = new MetaVocabulary(null, SettingsWithFloors(9), 1,
                new StubHeatLens { CurrentHeat = 0 });

            Assert.IsTrue(vocabulary.IsUnlocked("t", MetaGate.Base));
        }
    }
}
