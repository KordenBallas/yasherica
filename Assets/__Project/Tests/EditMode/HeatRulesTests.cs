using System.Collections.Generic;
using Heat.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HeatRulesTests
    {
        private static HeatPact Pact(params (string Id, int Rank)[] entries)
        {
            var pairs = new List<KeyValuePair<string, int>>();
            foreach (var (id, rank) in entries)
            {
                pairs.Add(new KeyValuePair<string, int>(id, rank));
            }

            return HeatPact.From(HeatPactTests.DemoSettings(), pairs);
        }

        [Test]
        public void EachKind_LandsInItsRule()
        {
            var rules = HeatRules.From(HeatPactTests.DemoSettings(), Pact(
                ("enemies-first", 1), ("raised-floor", 2), ("stingy-cauldron", 2), ("fewer-sockets", 1)));

            Assert.IsTrue(rules.EnemiesAlwaysLead);
            Assert.AreEqual(2, rules.EscalationTierLift); // magnitudes of steps 1..2 summed
            Assert.AreEqual(2, rules.VariantOptionCut);
            Assert.AreEqual(1, rules.SocketCut);
            Assert.AreEqual(11, rules.TotalHeat); // 2 + (2+2) + (1+2) + 2
        }

        [Test]
        public void EmptyPact_IsNeutral()
        {
            Assert.AreSame(HeatRules.Neutral, HeatRules.From(HeatPactTests.DemoSettings(), HeatPact.None));
            Assert.AreSame(HeatRules.Neutral, HeatRules.From(null, HeatPact.None));
            Assert.IsFalse(HeatRules.Neutral.EnemiesAlwaysLead);
            Assert.AreEqual(0, HeatRules.Neutral.EscalationTierLift);
            Assert.AreEqual(0, HeatRules.Neutral.VariantOptionCut);
            Assert.AreEqual(0, HeatRules.Neutral.SocketCut);
        }

        [Test]
        public void SameInputs_ComposeIdentically()
        {
            var first = HeatRules.From(HeatPactTests.DemoSettings(), Pact(("raised-floor", 1)));
            var second = HeatRules.From(HeatPactTests.DemoSettings(), Pact(("raised-floor", 1)));

            Assert.AreEqual(first.TotalHeat, second.TotalHeat);
            Assert.AreEqual(first.EscalationTierLift, second.EscalationTierLift);
            Assert.AreEqual(first.EnemiesAlwaysLead, second.EnemiesAlwaysLead);
        }
    }
}
