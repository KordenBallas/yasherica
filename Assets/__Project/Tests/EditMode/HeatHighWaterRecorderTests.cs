using System.Collections.Generic;
using Core.Persistence;
using Heat.Core;
using Heat.Integration;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HeatHighWaterRecorderTests
    {
        private static HeatRules Rules(int totalHeat)
        {
            if (totalHeat == 0)
            {
                return HeatRules.Neutral;
            }

            // raised-floor rank 1 = 2 heat; stack stingy-cauldron rank 1 for 3.
            var entries = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("raised-floor", 1)
            };
            if (totalHeat >= 3)
            {
                entries.Add(new KeyValuePair<string, int>("stingy-cauldron", 1));
            }

            return HeatRules.From(
                HeatPactTests.DemoSettings(), HeatPact.From(HeatPactTests.DemoSettings(), entries));
        }

        private static RunSaveSnapshot AtWindow(int windowIndex) => new RunSaveSnapshot
        {
            World = { WindowIndex = windowIndex }
        };

        [Test]
        public void HotRun_AtTheClearFloor_WritesTheRecord()
        {
            var facts = new FactStore();
            var recorder = new HeatHighWaterRecorder(facts, Rules(2), HeatPactTests.DemoSettings());

            recorder.OnSavepointCaptured(AtWindow(2)); // ClearWindowFloor = 2

            Assert.AreEqual(2, facts.GetInt(WorldFacts.HeatHighWater));
        }

        [Test]
        public void BeforeTheClearFloor_NothingIsWritten()
        {
            var facts = new FactStore();
            var recorder = new HeatHighWaterRecorder(facts, Rules(2), HeatPactTests.DemoSettings());

            recorder.OnSavepointCaptured(AtWindow(1));

            Assert.IsFalse(facts.Has(WorldFacts.HeatHighWater.ToKey()));
        }

        [Test]
        public void TheRecord_NeverLowers()
        {
            var facts = new FactStore();
            facts.SetInt(WorldFacts.HeatHighWater, 5);
            var recorder = new HeatHighWaterRecorder(facts, Rules(3), HeatPactTests.DemoSettings());

            recorder.OnSavepointCaptured(AtWindow(4));

            Assert.AreEqual(5, facts.GetInt(WorldFacts.HeatHighWater));
        }

        [Test]
        public void HeatZero_NeverWrites()
        {
            var facts = new FactStore();
            var recorder = new HeatHighWaterRecorder(facts, Rules(0), HeatPactTests.DemoSettings());

            recorder.OnSavepointCaptured(AtWindow(9));

            Assert.IsFalse(facts.Has(WorldFacts.HeatHighWater.ToKey()));
        }
    }
}
