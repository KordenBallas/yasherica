using System.Collections.Generic;
using Inventory.Core;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class SocketingTrendEvaluatorTests
    {
        private static readonly FusionSettings Fusion = new FusionSettings(1, 1f, 0.25f, 0.5f);

        private InventoryModel _inventory;
        private BlankRack _rack;
        private SocketingModel _socketing;
        private BlankInstance _skull;
        private FakeArtifactTraitSource _traits;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
            _rack = new BlankRack(2);
            _socketing = new SocketingModel(
                _inventory, _rack,
                new FakePartBlankDataSource().Add("blank.skull", "slot.head", "reptile", 2));
            _rack.TryAdd("blank.skull", out _skull);
            _traits = new FakeArtifactTraitSource()
                .Add("mace", 2, "stone", "heavy")
                .Add("stinger", 2, "sharp", "toxic");
        }

        private SocketingTrendEvaluator CreateEvaluator(TraitFusionRuleSet rules = null)
        {
            return new SocketingTrendEvaluator(
                _socketing, _traits, new EmergentFusionCalculator(),
                rules ?? TraitFusionRuleSet.Empty, Fusion);
        }

        [Test]
        public void Socketing_RaisesTrendWithCombinedTraits()
        {
            using (var evaluator = CreateEvaluator())
            {
                SocketingTrend trend = null;
                evaluator.OnTrendChanged += t => trend = t;

                var mace = _inventory.Add("mace");
                _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);

                Assert.IsNotNull(trend);
                Assert.AreEqual(_skull.InstanceId, trend.BlankInstanceId);
                CollectionAssert.AreEquivalent(new[] { "heavy", "stone" }, (ICollection<string>)trend.TraitIds);
                Assert.AreEqual(2, trend.Tier);
            }
        }

        [Test]
        public void Trend_AppliesTheFusionGrammar()
        {
            var rules = new TraitFusionRuleSet(new List<TraitFusionRule>
            {
                new TraitFusionRule("venom", new[] { "sharp", "toxic" }, new[] { "venomous" }, null, 0)
            });

            using (var evaluator = CreateEvaluator(rules))
            {
                SocketingTrend trend = null;
                evaluator.OnTrendChanged += t => trend = t;

                var stinger = _inventory.Add("stinger");
                _socketing.TrySocket(_skull.InstanceId, stinger.InstanceId);

                Assert.IsTrue(((IList<string>)new List<string>(trend.TraitIds)).Contains("venomous"));
            }
        }

        [Test]
        public void Unsocketing_RaisesEmptyTrend()
        {
            using (var evaluator = CreateEvaluator())
            {
                var mace = _inventory.Add("mace");
                _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);

                SocketingTrend trend = null;
                evaluator.OnTrendChanged += t => trend = t;
                _socketing.TryUnsocket(_skull.InstanceId, mace.InstanceId);

                Assert.IsNotNull(trend);
                Assert.AreEqual(0, trend.TraitIds.Count);
                Assert.AreEqual(0, trend.Tier);
            }
        }

        [Test]
        public void Dispose_StopsListening()
        {
            var evaluator = CreateEvaluator();
            SocketingTrend trend = null;
            evaluator.OnTrendChanged += t => trend = t;
            evaluator.Dispose();

            var mace = _inventory.Add("mace");
            _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);

            Assert.IsNull(trend);
        }
    }
}
