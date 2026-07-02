using System.Collections.Generic;
using Inventory.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class TraitFusionRuleSetTests
    {
        private static TraitFusionRule Rule(string id)
        {
            return new TraitFusionRule(id, new[] { "sharp" }, null, null, 0);
        }

        [Test]
        public void Rules_AreOrderedOrdinallyByRuleId()
        {
            var set = new TraitFusionRuleSet(new List<TraitFusionRule> { Rule("zeta"), Rule("alpha") });

            Assert.AreEqual("alpha", set.Rules[0].RuleId);
            Assert.AreEqual("zeta", set.Rules[1].RuleId);
        }

        [Test]
        public void DuplicateRuleId_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(
                () => new TraitFusionRuleSet(new List<TraitFusionRule> { Rule("a"), Rule("a") }));
        }

        [Test]
        public void NullEntries_AreSkipped()
        {
            var set = new TraitFusionRuleSet(new List<TraitFusionRule> { null, Rule("a") });

            Assert.AreEqual(1, set.Rules.Count);
        }

        [Test]
        public void Rule_WithoutRequiredTraits_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => new TraitFusionRule("bad", new string[0], null, null, 0));
        }

        [Test]
        public void Rule_WithEmptyId_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => new TraitFusionRule("", new[] { "sharp" }, null, null, 0));
        }
    }
}
