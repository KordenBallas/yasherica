using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class EmergentFusionCalculatorTests
    {
        private static readonly FusionSettings Settings = new FusionSettings(1, 1f, 0.25f, 0.5f);

        private EmergentFusionCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new EmergentFusionCalculator();
        }

        private static ArtifactTraitProfile Profile(int tier, params string[] traits)
        {
            return ArtifactTraitProfile.Create(traits, tier);
        }

        private static TraitFusionRuleSet Rules(params TraitFusionRule[] rules)
        {
            return new TraitFusionRuleSet(new List<TraitFusionRule>(rules));
        }

        [Test]
        public void ComputeTarget_UnionsInputTraits()
        {
            var target = _calculator.ComputeTarget(
                new[] { Profile(0, "fire", "fiery"), Profile(0, "stone", "heavy") },
                TraitFusionRuleSet.Empty, Settings);

            Assert.AreEqual(4, target.Traits.Count);
            Assert.IsTrue(target.Has("fire"));
            Assert.IsTrue(target.Has("fiery"));
            Assert.IsTrue(target.Has("stone"));
            Assert.IsTrue(target.Has("heavy"));
        }

        [Test]
        public void ComputeTarget_BaseTierIsMaxInputTier()
        {
            var target = _calculator.ComputeTarget(
                new[] { Profile(2, "sharp"), Profile(0, "heavy") },
                TraitFusionRuleSet.Empty, Settings);

            Assert.AreEqual(2, target.Tier);
        }

        [Test]
        public void ComputeTarget_DuplicateTraitAcrossInputs_AmplifiesTier()
        {
            var target = _calculator.ComputeTarget(
                new[] { Profile(0, "toxic", "rot"), Profile(0, "toxic", "water") },
                TraitFusionRuleSet.Empty, Settings);

            // One duplicated trait (toxic) x AmplifyTierBonus(1).
            Assert.AreEqual(1, target.Tier);
        }

        [Test]
        public void ComputeTarget_RuleFires_AddsAndRemovesTraits()
        {
            var rules = Rules(new TraitFusionRule(
                "transmute", new[] { "focusing", "fiery" }, new[] { "beaming" }, new[] { "fiery" }, 1));

            var target = _calculator.ComputeTarget(
                new[] { Profile(0, "focusing"), Profile(0, "fiery") },
                rules, Settings);

            Assert.IsTrue(target.Has("beaming"));
            Assert.IsFalse(target.Has("fiery"));
            Assert.IsTrue(target.Has("focusing"));
            Assert.AreEqual(1, target.Tier);
        }

        [Test]
        public void ComputeTarget_RuleWithMissingRequirement_DoesNotFire()
        {
            var rules = Rules(new TraitFusionRule(
                "transmute", new[] { "focusing", "fiery" }, new[] { "beaming" }, null, 0));

            var target = _calculator.ComputeTarget(
                new[] { Profile(0, "focusing") }, rules, Settings);

            Assert.IsFalse(target.Has("beaming"));
        }

        [Test]
        public void ComputeTarget_RulesCascadeInOrdinalIdOrder()
        {
            // "a_first" produces the trait "b_second" requires - and rules apply
            // in ordinal id order, so the cascade fires within one combine.
            var rules = Rules(
                new TraitFusionRule("a_first", new[] { "rot" }, new[] { "toxic" }, null, 0),
                new TraitFusionRule("b_second", new[] { "toxic" }, new[] { "corrosive" }, null, 0));

            var target = _calculator.ComputeTarget(new[] { Profile(0, "rot") }, rules, Settings);

            Assert.IsTrue(target.Has("toxic"));
            Assert.IsTrue(target.Has("corrosive"));
        }

        [Test]
        public void ComputeTarget_RuleOrderedAfterItsDependency_DoesNotFireBackwards()
        {
            // "a_first" requires the trait only "z_last" adds; by the time z_last
            // fires, a_first was already evaluated - no re-scan, deterministic.
            var rules = Rules(
                new TraitFusionRule("a_first", new[] { "toxic" }, new[] { "corrosive" }, null, 0),
                new TraitFusionRule("z_last", new[] { "rot" }, new[] { "toxic" }, null, 0));

            var target = _calculator.ComputeTarget(new[] { Profile(0, "rot") }, rules, Settings);

            Assert.IsTrue(target.Has("toxic"));
            Assert.IsFalse(target.Has("corrosive"));
        }

        [Test]
        public void ComputeTarget_NegativeRuleDelta_CannotDropTierBelowZero()
        {
            var rules = Rules(new TraitFusionRule("drain", new[] { "rot" }, null, null, -5));

            var target = _calculator.ComputeTarget(new[] { Profile(1, "rot") }, rules, Settings);

            Assert.AreEqual(0, target.Tier);
        }

        [Test]
        public void ComputeTarget_EmptyInputs_YieldsEmptyProfile()
        {
            var target = _calculator.ComputeTarget(
                new ArtifactTraitProfile[0], TraitFusionRuleSet.Empty, Settings);

            Assert.AreEqual(0, target.Traits.Count);
            Assert.AreEqual(0, target.Tier);
        }
    }
}
