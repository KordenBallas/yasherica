using MetaProgression.Core;
using MetaProgression.Data;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MetaProgressionConfigMapperTests
    {
        [Test]
        public void NullConfig_MapsToDefaults()
        {
            var settings = MetaProgressionConfigMapper.ToSettings(null);

            Assert.AreEqual(MetaProgressionSettings.Defaults.DirectionWindowRuns, settings.DirectionWindowRuns);
            Assert.AreEqual(MetaProgressionSettings.Defaults.BiasCeiling, settings.BiasCeiling);
            Assert.AreEqual(MetaProgressionSettings.Defaults.DigOfferSize, settings.DigOfferSize);
        }

        [Test]
        public void SettingsCtor_ClampsCeilingBelowCertainty()
        {
            var settings = new MetaProgressionSettings(null, 4, 1f, 1f, 1f, 5f, 3, false, 1f, 8);

            Assert.LessOrEqual(settings.BiasCeiling, MetaProgressionSettings.MaxBiasCeiling);
            Assert.Less(settings.BiasCeiling, 1f);
        }

        [Test]
        public void SettingsCtor_KeepsRetentionAtLeastTheWindow()
        {
            var settings = new MetaProgressionSettings(null, 5, 1f, 1f, 1f, 0.5f, 3, false, 1f, 2);

            Assert.GreaterOrEqual(settings.LedgerRetentionRuns, settings.DirectionWindowRuns);
        }

        [Test]
        public void SettingsCtor_EmptyFloors_FallBackToDefaultCurve()
        {
            var settings = new MetaProgressionSettings(new int[0], 4, 1f, 1f, 1f, 0.5f, 3, false, 1f, 8);

            Assert.Greater(settings.UnlockTierRunFloors.Count, 0);
            Assert.AreEqual(1, settings.FloorForTier(0));
        }

        [Test]
        public void FloorForTier_ClampsOutOfRangeTiers()
        {
            var settings = new MetaProgressionSettings(new[] { 1, 3, 7 }, 4, 1f, 1f, 1f, 0.5f, 3, false, 1f, 8);

            Assert.AreEqual(1, settings.FloorForTier(-2));
            Assert.AreEqual(7, settings.FloorForTier(2));
            Assert.AreEqual(7, settings.FloorForTier(99));
        }
    }
}
