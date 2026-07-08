using Heat.Core;
using MetaProgression.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HeatDialAdjusterTests
    {
        private static MetaProgressionSettings Base(float biasStrength = 1f, float ceiling = 0.75f,
            bool reserveSlot = false) =>
            new MetaProgressionSettings(null, 4, 1f, 1f, biasStrength, ceiling, 3, reserveSlot, 1f, 8);

        [Test]
        public void HeatZeroOrNullSettings_ReturnTheBaseInstance()
        {
            var baseSettings = Base();

            Assert.AreSame(baseSettings, HeatDialAdjuster.Apply(baseSettings, HeatPactTests.DemoSettings(), 0));
            Assert.AreSame(baseSettings, HeatDialAdjuster.Apply(baseSettings, null, 5));
        }

        [Test]
        public void BiasStrength_LiftsPerHeat_CeilingUntouched()
        {
            var adjusted = HeatDialAdjuster.Apply(Base(biasStrength: 1f), HeatPactTests.DemoSettings(), 4);

            Assert.AreEqual(1f + 0.15f * 4, adjusted.BiasStrength, 1e-5f);
            Assert.AreEqual(0.75f, adjusted.BiasCeiling, 1e-5f);
        }

        [Test]
        public void AbsurdHeat_NeverLiftsTheCeilingPastTheClamp()
        {
            // The never-guarantee invariant (R FR9): whatever the heat, the ceiling stays the
            // authored value and the constructor clamp (< 1) holds by construction.
            var adjusted = HeatDialAdjuster.Apply(Base(ceiling: 0.95f), HeatPactTests.DemoSettings(), 10000);

            Assert.LessOrEqual(adjusted.BiasCeiling, MetaProgressionSettings.MaxBiasCeiling);
            Assert.AreEqual(0.95f, adjusted.BiasCeiling, 1e-5f);
        }

        [Test]
        public void ReserveDirectionSlot_EnablesAtTheThreshold()
        {
            var heat = HeatPactTests.DemoSettings(); // threshold 4

            Assert.IsFalse(HeatDialAdjuster.Apply(Base(), heat, 3).ReserveDirectionSlot);
            Assert.IsTrue(HeatDialAdjuster.Apply(Base(), heat, 4).ReserveDirectionSlot);
            Assert.IsTrue(HeatDialAdjuster.Apply(Base(reserveSlot: true), heat, 1).ReserveDirectionSlot,
                "a base-enabled slot is never disabled by heat");
        }

        [Test]
        public void EverythingElse_CopiesThrough()
        {
            var baseSettings = Base();
            var adjusted = HeatDialAdjuster.Apply(baseSettings, HeatPactTests.DemoSettings(), 2);

            Assert.AreEqual(baseSettings.DigOfferSize, adjusted.DigOfferSize);
            Assert.AreEqual(baseSettings.DirectionWindowRuns, adjusted.DirectionWindowRuns);
            Assert.AreEqual(baseSettings.DilutionExponent, adjusted.DilutionExponent);
            Assert.AreEqual(baseSettings.LedgerRetentionRuns, adjusted.LedgerRetentionRuns);
            CollectionAssert.AreEqual(baseSettings.UnlockTierRunFloors, adjusted.UnlockTierRunFloors);
        }
    }
}
