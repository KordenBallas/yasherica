using Heat.Core;
using Heat.Data;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class HeatConfigMapperTests
    {
        [Test]
        public void NullConfig_MapsToInertDefaults()
        {
            var settings = HeatConfigMapper.ToSettings(null);

            Assert.IsEmpty(settings.Modifiers);
            Assert.AreEqual(0f, settings.FloorReliefRunsPerHeat);
            Assert.AreEqual(0f, settings.BiasStrengthLiftPerHeat);
            Assert.AreEqual(0, settings.ReserveDirectionSlotMinHeat);
        }

        [Test]
        public void FreshConfigInstance_MapsWithoutModifiers()
        {
            // A fresh SO carries the serialized dial defaults and an empty menu — a valid,
            // effectively inert configuration (Heat 0 = today's game).
            var config = ScriptableObject.CreateInstance<HeatConfig>();
            try
            {
                var settings = HeatConfigMapper.ToSettings(config);

                Assert.IsEmpty(settings.Modifiers);
                Assert.GreaterOrEqual(settings.ClearWindowFloor, 0);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void AuthoredAsset_MapsTheDemonstratorMenu()
        {
            // The real authored asset: four modifiers over the four effect kinds.
            var config = Resources.Load<HeatConfig>("Configs/HeatConfig");
            Assert.IsNotNull(config, "demonstrator HeatConfig asset missing");

            var settings = HeatConfigMapper.ToSettings(config);

            Assert.AreEqual(4, settings.Modifiers.Count);
            Assert.IsTrue(settings.TryGetModifier("enemies-first", out var enemiesFirst));
            Assert.AreEqual(HeatEffectKind.EnemiesActFirst, enemiesFirst.Kind);
            Assert.AreEqual(1, enemiesFirst.MaxRank);
            Assert.IsTrue(settings.TryGetModifier("raised-floor", out var raisedFloor));
            Assert.AreEqual(2, raisedFloor.MaxRank);
            Assert.AreEqual(4, raisedFloor.HeatAtRank(2));
            // Owner call 2026-07-08: the demo cap is lifted (0 = uncapped) while the four-modifier
            // menu is playtested end-to-end; re-cap by data when the menu grows.
            Assert.AreEqual(0, settings.SoftCapTotalHeat);
            Assert.Greater(settings.ClearWindowFloor, 0);
        }
    }
}
